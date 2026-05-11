using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Results;
using NaijaPrimeSchool.Application.Results.Dtos;
using NaijaPrimeSchool.Domain.Results;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class AssessmentService(ApplicationDbContext db) : IAssessmentService
{
    private IQueryable<TermAssessmentDto> Project(IQueryable<TermAssessment> q) =>
        q.Select(a => new TermAssessmentDto
        {
            Id = a.Id,
            TermId = a.TermId,
            TermName = a.Term!.TermType!.Name + " — " + a.Term!.Session!.Name,
            SessionId = a.Term!.SessionId,
            SessionName = a.Term!.Session!.Name,
            SchoolClassId = a.SchoolClassId,
            SchoolClassName = a.SchoolClass!.Name,
            SubjectId = a.SubjectId,
            SubjectName = a.Subject!.Name,
            SubjectCode = a.Subject!.Code,
            AssessmentTypeId = a.AssessmentTypeId,
            AssessmentTypeName = a.AssessmentType!.Name,
            AssessmentTypeCode = a.AssessmentType!.Code,
            IsExam = a.AssessmentType!.IsExam,
            Title = a.Title,
            MaxScore = a.MaxScore,
            Weight = a.Weight,
            AssessmentDate = a.AssessmentDate,
            IsPublished = a.IsPublished,
            PublishedOn = a.PublishedOn,
            Notes = a.Notes,
            ScoredCount = a.Scores.Count,
            ExpectedCount = db.Enrolments
                .Count(e => e.SchoolClassId == a.SchoolClassId && e.WithdrawnOn == null),
        });

    public async Task<IReadOnlyList<TermAssessmentDto>> ListAsync(TermAssessmentFilter filter, CancellationToken ct = default)
    {
        var q = db.TermAssessments.AsQueryable();
        if (filter.TermId.HasValue) q = q.Where(a => a.TermId == filter.TermId.Value);
        if (filter.SchoolClassId.HasValue) q = q.Where(a => a.SchoolClassId == filter.SchoolClassId.Value);
        if (filter.SubjectId.HasValue) q = q.Where(a => a.SubjectId == filter.SubjectId.Value);
        if (filter.AssessmentTypeId.HasValue) q = q.Where(a => a.AssessmentTypeId == filter.AssessmentTypeId.Value);
        if (filter.IsPublished.HasValue) q = q.Where(a => a.IsPublished == filter.IsPublished.Value);

        return await Project(q
                .OrderBy(a => a.Subject!.Name)
                .ThenBy(a => a.AssessmentType!.DisplayOrder)
                .ThenByDescending(a => a.AssessmentDate))
            .ToListAsync(ct);
    }

    public Task<TermAssessmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Project(db.TermAssessments.Where(a => a.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateTermAssessmentRequest request, CancellationToken ct = default)
    {
        if (!await db.Terms.AnyAsync(t => t.Id == request.TermId, ct))
            return OperationResult<Guid>.Failure("Term not found.");

        if (!await db.SchoolClasses.AnyAsync(c => c.Id == request.SchoolClassId, ct))
            return OperationResult<Guid>.Failure("Class not found.");

        if (!await db.Subjects.AnyAsync(s => s.Id == request.SubjectId, ct))
            return OperationResult<Guid>.Failure("Subject not found.");

        if (!await db.AssessmentTypes.AnyAsync(t => t.Id == request.AssessmentTypeId, ct))
            return OperationResult<Guid>.Failure("Assessment type not found.");

        if (request.MaxScore <= 0)
            return OperationResult<Guid>.Failure("Maximum score must be greater than zero.");

        if (request.Weight < 0)
            return OperationResult<Guid>.Failure("Weight cannot be negative.");

        var assessment = new TermAssessment
        {
            TermId = request.TermId,
            SchoolClassId = request.SchoolClassId,
            SubjectId = request.SubjectId,
            AssessmentTypeId = request.AssessmentTypeId,
            Title = request.Title.Trim(),
            MaxScore = request.MaxScore,
            Weight = request.Weight,
            AssessmentDate = request.AssessmentDate,
            Notes = request.Notes,
        };

        db.TermAssessments.Add(assessment);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(assessment.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateTermAssessmentRequest request, CancellationToken ct = default)
    {
        var assessment = await db.TermAssessments.FirstOrDefaultAsync(a => a.Id == request.Id, ct);
        if (assessment is null) return OperationResult.Failure("Assessment not found.");

        if (assessment.IsPublished)
            return OperationResult.Failure("Unpublish the assessment before editing.");

        if (!await db.AssessmentTypes.AnyAsync(t => t.Id == request.AssessmentTypeId, ct))
            return OperationResult.Failure("Assessment type not found.");

        if (request.MaxScore <= 0)
            return OperationResult.Failure("Maximum score must be greater than zero.");

        if (request.Weight < 0)
            return OperationResult.Failure("Weight cannot be negative.");

        assessment.AssessmentTypeId = request.AssessmentTypeId;
        assessment.Title = request.Title.Trim();
        assessment.MaxScore = request.MaxScore;
        assessment.Weight = request.Weight;
        assessment.AssessmentDate = request.AssessmentDate;
        assessment.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.TermAssessments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return OperationResult.Failure("Assessment not found.");

        a.IsPublished = true;
        a.PublishedOn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UnpublishAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.TermAssessments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return OperationResult.Failure("Assessment not found.");

        a.IsPublished = false;
        a.PublishedOn = null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.TermAssessments
            .Include(x => x.Scores)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return OperationResult.Failure("Assessment not found.");

        if (a.IsPublished)
            return OperationResult.Failure("Unpublish the assessment before deleting.");

        if (a.Scores.Any())
            return OperationResult.Failure(
                "Cannot delete an assessment that already has scores. Clear the scores first.");

        db.TermAssessments.Remove(a);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<AssessmentScoreSheetDto?> GetScoreSheetAsync(Guid assessmentId, CancellationToken ct = default)
    {
        var assessment = await Project(db.TermAssessments.Where(a => a.Id == assessmentId))
            .FirstOrDefaultAsync(ct);
        if (assessment is null) return null;

        var existing = await db.AssessmentScores
            .Where(s => s.TermAssessmentId == assessmentId)
            .Select(s => new
            {
                s.Id,
                s.StudentId,
                s.Score,
                s.IsAbsent,
                s.Remarks,
            })
            .ToListAsync(ct);

        var enrolledStudents = await db.Enrolments
            .Where(e => e.SchoolClassId == assessment.SchoolClassId && e.WithdrawnOn == null)
            .Select(e => new
            {
                e.StudentId,
                FirstName = e.Student!.FirstName,
                LastName = e.Student!.LastName,
                Name = e.Student!.FirstName + " " + e.Student!.LastName,
                AdmissionNumber = e.Student!.AdmissionNumber,
                PhotoUrl = e.Student!.PhotoUrl,
            })
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        var rows = enrolledStudents
            .Select(s =>
            {
                var match = existing.FirstOrDefault(x => x.StudentId == s.StudentId);
                return new AssessmentScoreDto
                {
                    Id = match?.Id ?? Guid.Empty,
                    TermAssessmentId = assessmentId,
                    StudentId = s.StudentId,
                    StudentName = s.Name.Trim(),
                    StudentAdmissionNumber = s.AdmissionNumber,
                    StudentPhotoUrl = s.PhotoUrl,
                    StudentFirstName = s.FirstName,
                    StudentLastName = s.LastName,
                    Score = match?.Score,
                    IsAbsent = match?.IsAbsent ?? false,
                    Remarks = match?.Remarks,
                };
            })
            .ToList();

        return new AssessmentScoreSheetDto { Assessment = assessment, Scores = rows };
    }

    public async Task<OperationResult> UpsertScoreAsync(UpsertAssessmentScoreRequest request, CancellationToken ct = default)
    {
        var assessment = await db.TermAssessments
            .FirstOrDefaultAsync(a => a.Id == request.TermAssessmentId, ct);
        if (assessment is null) return OperationResult.Failure("Assessment not found.");
        if (assessment.IsPublished)
            return OperationResult.Failure("Unpublish the assessment before editing scores.");

        if (request.Score.HasValue && (request.Score.Value < 0 || request.Score.Value > assessment.MaxScore))
            return OperationResult.Failure(
                $"Score must be between 0 and {assessment.MaxScore}.");

        var score = await db.AssessmentScores
            .FirstOrDefaultAsync(s =>
                s.TermAssessmentId == request.TermAssessmentId
                && s.StudentId == request.StudentId, ct);

        if (score is null)
        {
            score = new AssessmentScore
            {
                TermAssessmentId = request.TermAssessmentId,
                StudentId = request.StudentId,
                Score = request.IsAbsent ? null : request.Score,
                IsAbsent = request.IsAbsent,
                Remarks = request.Remarks,
            };
            db.AssessmentScores.Add(score);
        }
        else
        {
            score.Score = request.IsAbsent ? null : request.Score;
            score.IsAbsent = request.IsAbsent;
            score.Remarks = request.Remarks;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> BulkSetScoresAsync(BulkSetScoresRequest request, CancellationToken ct = default)
    {
        var assessment = await db.TermAssessments
            .FirstOrDefaultAsync(a => a.Id == request.TermAssessmentId, ct);
        if (assessment is null) return OperationResult.Failure("Assessment not found.");
        if (assessment.IsPublished)
            return OperationResult.Failure("Unpublish the assessment before editing scores.");

        var existing = await db.AssessmentScores
            .Where(s => s.TermAssessmentId == request.TermAssessmentId)
            .ToListAsync(ct);

        var errors = new List<string>();
        foreach (var row in request.Scores)
        {
            if (row.Score.HasValue && (row.Score.Value < 0 || row.Score.Value > assessment.MaxScore))
            {
                errors.Add($"Student {row.StudentId}: score out of range (0..{assessment.MaxScore}).");
                continue;
            }

            var existingRow = existing.FirstOrDefault(s => s.StudentId == row.StudentId);
            if (existingRow is null)
            {
                if (row.Score is null && !row.IsAbsent && string.IsNullOrWhiteSpace(row.Remarks))
                    continue; // nothing to write

                db.AssessmentScores.Add(new AssessmentScore
                {
                    TermAssessmentId = request.TermAssessmentId,
                    StudentId = row.StudentId,
                    Score = row.IsAbsent ? null : row.Score,
                    IsAbsent = row.IsAbsent,
                    Remarks = row.Remarks,
                });
            }
            else
            {
                existingRow.Score = row.IsAbsent ? null : row.Score;
                existingRow.IsAbsent = row.IsAbsent;
                existingRow.Remarks = row.Remarks;
            }
        }

        if (errors.Count > 0) return OperationResult.Failure(errors);

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
