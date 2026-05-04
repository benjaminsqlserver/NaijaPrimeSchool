using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Results;
using NaijaPrimeSchool.Application.Results.Dtos;
using NaijaPrimeSchool.Domain.Results;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class ResultService(ApplicationDbContext db) : IResultService
{
    private static IQueryable<SubjectResultDto> Project(IQueryable<SubjectResult> q) =>
        q.Select(r => new SubjectResultDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            StudentName = (r.Student!.FirstName + " " + r.Student!.LastName).Trim(),
            StudentAdmissionNumber = r.Student!.AdmissionNumber,
            TermId = r.TermId,
            TermName = r.Term!.TermType!.Name + " — " + r.Term!.Session!.Name,
            SubjectId = r.SubjectId,
            SubjectName = r.Subject!.Name,
            SubjectCode = r.Subject!.Code,
            SchoolClassId = r.SchoolClassId,
            SchoolClassName = r.SchoolClass!.Name,
            TotalScore = r.TotalScore,
            Percentage = r.Percentage,
            GradeBandId = r.GradeBandId,
            GradeBandName = r.GradeBand == null ? null : r.GradeBand.Name,
            GradeBandRemark = r.GradeBand == null ? null : r.GradeBand.Remark,
            Position = r.Position,
            StudentsInClass = r.StudentsInClass,
            TeacherComment = r.TeacherComment,
            IsFinalised = r.IsFinalised,
            FinalisedOn = r.FinalisedOn,
        });

    public async Task<IReadOnlyList<SubjectResultDto>> ListAsync(SubjectResultFilter filter, CancellationToken ct = default)
    {
        var q = db.SubjectResults.AsQueryable();
        if (filter.StudentId.HasValue) q = q.Where(r => r.StudentId == filter.StudentId.Value);
        if (filter.TermId.HasValue) q = q.Where(r => r.TermId == filter.TermId.Value);
        if (filter.SubjectId.HasValue) q = q.Where(r => r.SubjectId == filter.SubjectId.Value);
        if (filter.SchoolClassId.HasValue) q = q.Where(r => r.SchoolClassId == filter.SchoolClassId.Value);
        if (filter.IsFinalised.HasValue) q = q.Where(r => r.IsFinalised == filter.IsFinalised.Value);

        return await Project(q
                .OrderBy(r => r.Subject!.Name)
                .ThenBy(r => r.Position ?? int.MaxValue)
                .ThenByDescending(r => r.Percentage))
            .ToListAsync(ct);
    }

    public Task<SubjectResultDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Project(db.SubjectResults.Where(r => r.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<OperationResult<ComputeResultsResponse>> ComputeAsync(
        ComputeResultsRequest request, CancellationToken ct = default)
    {
        if (!await db.Terms.AnyAsync(t => t.Id == request.TermId, ct))
            return OperationResult<ComputeResultsResponse>.Failure("Term not found.");

        if (!await db.SchoolClasses.AnyAsync(c => c.Id == request.SchoolClassId, ct))
            return OperationResult<ComputeResultsResponse>.Failure("Class not found.");

        var assessmentsQuery = db.TermAssessments
            .Where(a => a.TermId == request.TermId
                && a.SchoolClassId == request.SchoolClassId);
        if (request.SubjectId.HasValue)
            assessmentsQuery = assessmentsQuery.Where(a => a.SubjectId == request.SubjectId.Value);

        var assessments = await assessmentsQuery
            .Select(a => new
            {
                a.Id,
                a.SubjectId,
                a.MaxScore,
                a.Weight,
            })
            .ToListAsync(ct);

        if (assessments.Count == 0)
            return OperationResult<ComputeResultsResponse>.Failure(
                "No assessments exist for this term/class. Create assessments first.");

        var scoreRows = await db.AssessmentScores
            .Where(s => assessments.Select(a => a.Id).Contains(s.TermAssessmentId))
            .Select(s => new
            {
                s.TermAssessmentId,
                s.StudentId,
                s.Score,
                s.IsAbsent,
            })
            .ToListAsync(ct);

        // Active enrolments in the class for this term — these are the candidates.
        var enrolledStudents = await db.Enrolments
            .Where(e => e.SchoolClassId == request.SchoolClassId)
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync(ct);

        var bands = await db.GradeBands
            .OrderBy(g => g.DisplayOrder)
            .ToListAsync(ct);

        var subjectIds = assessments.Select(a => a.SubjectId).Distinct().ToList();
        var warnings = new List<string>();
        var rowsTouched = 0;
        var rowsFinalised = 0;

        // Build per-subject working data, then per-(student, subject) aggregate.
        foreach (var subjectId in subjectIds)
        {
            var subjectAssessments = assessments.Where(a => a.SubjectId == subjectId).ToList();
            // Total of (MaxScore × Weight) across all assessments — the denominator for the percentage.
            decimal subjectTotalWeighted = subjectAssessments.Sum(a => a.MaxScore * a.Weight);
            if (subjectTotalWeighted <= 0)
            {
                warnings.Add($"Subject {subjectId}: total weighted max is zero. Skipped.");
                continue;
            }

            var perStudent = new List<(Guid StudentId, decimal Total, decimal Percentage)>();

            foreach (var studentId in enrolledStudents)
            {
                decimal studentWeighted = 0m;
                foreach (var a in subjectAssessments)
                {
                    var score = scoreRows.FirstOrDefault(s =>
                        s.TermAssessmentId == a.Id && s.StudentId == studentId);
                    decimal raw = score?.Score ?? 0m;
                    studentWeighted += raw * a.Weight;
                }

                decimal percentage = Math.Round(studentWeighted * 100m / subjectTotalWeighted, 2);
                perStudent.Add((studentId, Math.Round(studentWeighted, 2), percentage));
            }

            // Compute positions for this subject (1-based, dense ranking on percentage).
            var ordered = perStudent
                .OrderByDescending(x => x.Percentage)
                .ToList();
            var positions = new Dictionary<Guid, int>();
            int currentPos = 0;
            decimal? prevPct = null;
            for (int i = 0; i < ordered.Count; i++)
            {
                var row = ordered[i];
                if (prevPct is null || row.Percentage != prevPct.Value)
                {
                    currentPos = i + 1;
                    prevPct = row.Percentage;
                }
                positions[row.StudentId] = currentPos;
            }

            var existingRows = await db.SubjectResults
                .Where(r => r.TermId == request.TermId
                    && r.SchoolClassId == request.SchoolClassId
                    && r.SubjectId == subjectId)
                .ToListAsync(ct);

            foreach (var row in perStudent)
            {
                var band = bands.FirstOrDefault(g =>
                    row.Percentage >= g.LowerBound && row.Percentage <= g.UpperBound);

                var existing = existingRows.FirstOrDefault(r => r.StudentId == row.StudentId);
                if (existing is null)
                {
                    var fresh = new SubjectResult
                    {
                        StudentId = row.StudentId,
                        TermId = request.TermId,
                        SubjectId = subjectId,
                        SchoolClassId = request.SchoolClassId,
                        TotalScore = row.Total,
                        Percentage = row.Percentage,
                        GradeBandId = band?.Id,
                        Position = positions[row.StudentId],
                        StudentsInClass = perStudent.Count,
                        IsFinalised = request.Finalise,
                        FinalisedOn = request.Finalise ? DateTimeOffset.UtcNow : null,
                    };
                    db.SubjectResults.Add(fresh);
                    rowsTouched++;
                    if (request.Finalise) rowsFinalised++;
                }
                else
                {
                    if (existing.IsFinalised && !request.Finalise)
                    {
                        warnings.Add(
                            $"Student {row.StudentId} subject {subjectId}: already finalised — skipped recompute.");
                        continue;
                    }

                    existing.TotalScore = row.Total;
                    existing.Percentage = row.Percentage;
                    existing.GradeBandId = band?.Id;
                    existing.Position = positions[row.StudentId];
                    existing.StudentsInClass = perStudent.Count;
                    if (request.Finalise && !existing.IsFinalised)
                    {
                        existing.IsFinalised = true;
                        existing.FinalisedOn = DateTimeOffset.UtcNow;
                        rowsFinalised++;
                    }
                    rowsTouched++;
                }
            }
        }

        await db.SaveChangesAsync(ct);

        return OperationResult<ComputeResultsResponse>.Success(new ComputeResultsResponse
        {
            RowsComputed = rowsTouched,
            RowsFinalised = rowsFinalised,
            Warnings = warnings,
        });
    }

    public async Task<OperationResult> UpdateCommentAsync(UpdateSubjectResultRequest request, CancellationToken ct = default)
    {
        var r = await db.SubjectResults.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (r is null) return OperationResult.Failure("Result not found.");

        r.TeacherComment = request.TeacherComment;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> FinaliseAsync(Guid id, CancellationToken ct = default)
    {
        var r = await db.SubjectResults.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return OperationResult.Failure("Result not found.");

        r.IsFinalised = true;
        r.FinalisedOn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ReopenAsync(Guid id, CancellationToken ct = default)
    {
        var r = await db.SubjectResults.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return OperationResult.Failure("Result not found.");

        r.IsFinalised = false;
        r.FinalisedOn = null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var r = await db.SubjectResults.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return OperationResult.Failure("Result not found.");

        if (r.IsFinalised)
            return OperationResult.Failure("Cannot delete a finalised result. Reopen it first.");

        db.SubjectResults.Remove(r);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
