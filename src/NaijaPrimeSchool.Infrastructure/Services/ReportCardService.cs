using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Results;
using NaijaPrimeSchool.Application.Results.Dtos;
using NaijaPrimeSchool.Domain.Results;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class ReportCardService(ApplicationDbContext db) : IReportCardService
{
    private static IQueryable<ReportCardDto> ProjectCard(IQueryable<ReportCard> q) =>
        q.Select(c => new ReportCardDto
        {
            Id = c.Id,
            StudentId = c.StudentId,
            StudentName = (c.Student!.FirstName + " " + c.Student!.LastName).Trim(),
            StudentAdmissionNumber = c.Student!.AdmissionNumber,
            TermId = c.TermId,
            TermName = c.Term!.TermType!.Name + " — " + c.Term!.Session!.Name,
            SessionId = c.Term!.SessionId,
            SessionName = c.Term!.Session!.Name,
            SchoolClassId = c.SchoolClassId,
            SchoolClassName = c.SchoolClass!.Name,
            SubjectsTaken = c.SubjectsTaken,
            TotalScore = c.TotalScore,
            AveragePercentage = c.AveragePercentage,
            Position = c.Position,
            StudentsInClass = c.StudentsInClass,
            DaysPresent = c.DaysPresent,
            DaysAbsent = c.DaysAbsent,
            DaysLate = c.DaysLate,
            TotalSchoolDays = c.TotalSchoolDays,
            ClassTeacherComment = c.ClassTeacherComment,
            HeadTeacherComment = c.HeadTeacherComment,
            NextTermBegins = c.NextTermBegins,
            IsPublished = c.IsPublished,
            PublishedOn = c.PublishedOn,
        });

    public async Task<IReadOnlyList<ReportCardDto>> ListAsync(ReportCardFilter filter, CancellationToken ct = default)
    {
        var q = db.ReportCards.AsQueryable();
        if (filter.TermId.HasValue) q = q.Where(c => c.TermId == filter.TermId.Value);
        if (filter.SchoolClassId.HasValue) q = q.Where(c => c.SchoolClassId == filter.SchoolClassId.Value);
        if (filter.StudentId.HasValue) q = q.Where(c => c.StudentId == filter.StudentId.Value);
        if (filter.IsPublished.HasValue) q = q.Where(c => c.IsPublished == filter.IsPublished.Value);

        return await ProjectCard(q
                .OrderBy(c => c.Position ?? int.MaxValue)
                .ThenBy(c => c.Student!.FirstName))
            .ToListAsync(ct);
    }

    public async Task<ReportCardDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var card = await ProjectCard(db.ReportCards.Where(c => c.Id == id))
            .FirstOrDefaultAsync(ct);
        if (card is null) return null;
        return await BuildDetailAsync(card, ct);
    }

    public async Task<ReportCardDetailDto?> GetForStudentTermAsync(Guid studentId, Guid termId, CancellationToken ct = default)
    {
        var card = await ProjectCard(db.ReportCards
                .Where(c => c.StudentId == studentId && c.TermId == termId))
            .FirstOrDefaultAsync(ct);
        if (card is null) return null;
        return await BuildDetailAsync(card, ct);
    }

    private async Task<ReportCardDetailDto> BuildDetailAsync(ReportCardDto card, CancellationToken ct)
    {
        var results = await db.SubjectResults
            .Where(r => r.StudentId == card.StudentId && r.TermId == card.TermId)
            .OrderBy(r => r.Subject!.Name)
            .Select(r => new SubjectResultDto
            {
                Id = r.Id,
                StudentId = r.StudentId,
                StudentName = card.StudentName,
                StudentAdmissionNumber = card.StudentAdmissionNumber,
                TermId = r.TermId,
                TermName = card.TermName,
                SubjectId = r.SubjectId,
                SubjectName = r.Subject!.Name,
                SubjectCode = r.Subject!.Code,
                SchoolClassId = r.SchoolClassId,
                SchoolClassName = card.SchoolClassName,
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
            })
            .ToListAsync(ct);

        var affective = await db.AffectiveRatings
            .Where(r => r.ReportCardId == card.Id)
            .OrderBy(r => r.AffectiveTrait!.DisplayOrder)
            .Select(r => new AffectiveRatingDto
            {
                Id = r.Id,
                AffectiveTraitId = r.AffectiveTraitId,
                AffectiveTraitName = r.AffectiveTrait!.Name,
                TraitRatingId = r.TraitRatingId,
                TraitRatingName = r.TraitRating!.Name,
                TraitRatingValue = r.TraitRating!.Value,
            })
            .ToListAsync(ct);

        var psycho = await db.PsychomotorRatings
            .Where(r => r.ReportCardId == card.Id)
            .OrderBy(r => r.PsychomotorSkill!.DisplayOrder)
            .Select(r => new PsychomotorRatingDto
            {
                Id = r.Id,
                PsychomotorSkillId = r.PsychomotorSkillId,
                PsychomotorSkillName = r.PsychomotorSkill!.Name,
                TraitRatingId = r.TraitRatingId,
                TraitRatingName = r.TraitRating!.Name,
                TraitRatingValue = r.TraitRating!.Value,
            })
            .ToListAsync(ct);

        return new ReportCardDetailDto
        {
            Card = card,
            Results = results,
            AffectiveRatings = affective,
            PsychomotorRatings = psycho,
        };
    }

    public async Task<OperationResult<GenerateReportCardsResponse>> GenerateAsync(
        GenerateReportCardsRequest request, CancellationToken ct = default)
    {
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == request.TermId, ct);
        if (term is null) return OperationResult<GenerateReportCardsResponse>.Failure("Term not found.");

        if (!await db.SchoolClasses.AnyAsync(c => c.Id == request.SchoolClassId, ct))
            return OperationResult<GenerateReportCardsResponse>.Failure("Class not found.");

        var resultsByStudent = await db.SubjectResults
            .Where(r => r.TermId == request.TermId && r.SchoolClassId == request.SchoolClassId)
            .GroupBy(r => r.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Subjects = g.Count(),
                TotalScore = g.Sum(r => r.Percentage),
                Average = g.Average(r => r.Percentage),
            })
            .ToListAsync(ct);

        if (resultsByStudent.Count == 0)
            return OperationResult<GenerateReportCardsResponse>.Failure(
                "No subject results exist for this term/class. Compute results first.");

        // Per-student attendance summary across this term, restricted to this class.
        // Materialise the flattened rows first, then aggregate in memory — this
        // avoids EF Core translation surprises with multiple conditional Counts
        // inside a single GroupBy projection.
        var entryRows = await db.DailyAttendanceEntries
            .Where(e => e.Register!.TermId == request.TermId
                && e.Register!.SchoolClassId == request.SchoolClassId)
            .Select(e => new
            {
                e.StudentId,
                Code = e.AttendanceStatus!.Code,
                CountsAsPresent = e.AttendanceStatus!.CountsAsPresent,
            })
            .ToListAsync(ct);

        var attendance = entryRows
            .GroupBy(e => e.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Total = g.Count(),
                Present = g.Count(x => x.CountsAsPresent && x.Code != "L"),
                Late = g.Count(x => x.Code == "L"),
                Absent = g.Count(x => !x.CountsAsPresent),
            })
            .ToList();

        var totalSchoolDays = await db.DailyAttendanceRegisters
            .Where(r => r.TermId == request.TermId && r.SchoolClassId == request.SchoolClassId)
            .CountAsync(ct);

        // Position by average percentage.
        var ordered = resultsByStudent
            .OrderByDescending(x => x.Average)
            .ToList();
        var positions = new Dictionary<Guid, int>();
        int currentPos = 0;
        decimal? prevAvg = null;
        for (int i = 0; i < ordered.Count; i++)
        {
            if (prevAvg is null || ordered[i].Average != prevAvg.Value)
            {
                currentPos = i + 1;
                prevAvg = ordered[i].Average;
            }
            positions[ordered[i].StudentId] = currentPos;
        }

        var existing = await db.ReportCards
            .Where(c => c.TermId == request.TermId && c.SchoolClassId == request.SchoolClassId)
            .ToListAsync(ct);

        var generated = 0;
        var updated = 0;

        foreach (var s in resultsByStudent)
        {
            var att = attendance.FirstOrDefault(a => a.StudentId == s.StudentId);
            var card = existing.FirstOrDefault(c => c.StudentId == s.StudentId);

            if (card is null)
            {
                card = new ReportCard
                {
                    StudentId = s.StudentId,
                    TermId = request.TermId,
                    SchoolClassId = request.SchoolClassId,
                    SubjectsTaken = s.Subjects,
                    TotalScore = Math.Round(s.TotalScore, 2),
                    AveragePercentage = Math.Round(s.Average, 2),
                    Position = positions[s.StudentId],
                    StudentsInClass = resultsByStudent.Count,
                    DaysPresent = att?.Present ?? 0,
                    DaysAbsent = att?.Absent ?? 0,
                    DaysLate = att?.Late ?? 0,
                    TotalSchoolDays = totalSchoolDays,
                    NextTermBegins = request.NextTermBegins,
                };
                db.ReportCards.Add(card);
                generated++;
            }
            else
            {
                if (card.IsPublished)
                    continue; // never overwrite a published card
                card.SubjectsTaken = s.Subjects;
                card.TotalScore = Math.Round(s.TotalScore, 2);
                card.AveragePercentage = Math.Round(s.Average, 2);
                card.Position = positions[s.StudentId];
                card.StudentsInClass = resultsByStudent.Count;
                card.DaysPresent = att?.Present ?? 0;
                card.DaysAbsent = att?.Absent ?? 0;
                card.DaysLate = att?.Late ?? 0;
                card.TotalSchoolDays = totalSchoolDays;
                if (request.NextTermBegins.HasValue) card.NextTermBegins = request.NextTermBegins;
                updated++;
            }
        }

        await db.SaveChangesAsync(ct);

        return OperationResult<GenerateReportCardsResponse>.Success(new GenerateReportCardsResponse
        {
            CardsGenerated = generated,
            CardsUpdated = updated,
            Warnings = [],
        });
    }

    public async Task<OperationResult> UpdateCommentsAsync(UpdateReportCardCommentsRequest request, CancellationToken ct = default)
    {
        var card = await db.ReportCards.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (card is null) return OperationResult.Failure("Report card not found.");
        if (card.IsPublished) return OperationResult.Failure("Unpublish the card before editing.");

        card.ClassTeacherComment = request.ClassTeacherComment;
        card.HeadTeacherComment = request.HeadTeacherComment;
        if (request.NextTermBegins.HasValue) card.NextTermBegins = request.NextTermBegins;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpsertAffectiveRatingAsync(UpsertAffectiveRatingRequest request, CancellationToken ct = default)
    {
        var card = await db.ReportCards.FirstOrDefaultAsync(c => c.Id == request.ReportCardId, ct);
        if (card is null) return OperationResult.Failure("Report card not found.");
        if (card.IsPublished) return OperationResult.Failure("Unpublish the card before editing ratings.");

        if (!await db.AffectiveTraits.AnyAsync(t => t.Id == request.AffectiveTraitId, ct))
            return OperationResult.Failure("Affective trait not found.");

        if (!await db.TraitRatings.AnyAsync(r => r.Id == request.TraitRatingId, ct))
            return OperationResult.Failure("Trait rating not found.");

        var existing = await db.AffectiveRatings.FirstOrDefaultAsync(r =>
            r.ReportCardId == request.ReportCardId
            && r.AffectiveTraitId == request.AffectiveTraitId, ct);

        if (existing is null)
        {
            db.AffectiveRatings.Add(new AffectiveRating
            {
                ReportCardId = request.ReportCardId,
                AffectiveTraitId = request.AffectiveTraitId,
                TraitRatingId = request.TraitRatingId,
            });
        }
        else
        {
            existing.TraitRatingId = request.TraitRatingId;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpsertPsychomotorRatingAsync(UpsertPsychomotorRatingRequest request, CancellationToken ct = default)
    {
        var card = await db.ReportCards.FirstOrDefaultAsync(c => c.Id == request.ReportCardId, ct);
        if (card is null) return OperationResult.Failure("Report card not found.");
        if (card.IsPublished) return OperationResult.Failure("Unpublish the card before editing ratings.");

        if (!await db.PsychomotorSkills.AnyAsync(s => s.Id == request.PsychomotorSkillId, ct))
            return OperationResult.Failure("Psychomotor skill not found.");

        if (!await db.TraitRatings.AnyAsync(r => r.Id == request.TraitRatingId, ct))
            return OperationResult.Failure("Trait rating not found.");

        var existing = await db.PsychomotorRatings.FirstOrDefaultAsync(r =>
            r.ReportCardId == request.ReportCardId
            && r.PsychomotorSkillId == request.PsychomotorSkillId, ct);

        if (existing is null)
        {
            db.PsychomotorRatings.Add(new PsychomotorRating
            {
                ReportCardId = request.ReportCardId,
                PsychomotorSkillId = request.PsychomotorSkillId,
                TraitRatingId = request.TraitRatingId,
            });
        }
        else
        {
            existing.TraitRatingId = request.TraitRatingId;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var card = await db.ReportCards.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (card is null) return OperationResult.Failure("Report card not found.");

        card.IsPublished = true;
        card.PublishedOn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UnpublishAsync(Guid id, CancellationToken ct = default)
    {
        var card = await db.ReportCards.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (card is null) return OperationResult.Failure("Report card not found.");

        card.IsPublished = false;
        card.PublishedOn = null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var card = await db.ReportCards.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (card is null) return OperationResult.Failure("Report card not found.");

        if (card.IsPublished)
            return OperationResult.Failure("Unpublish the card before deleting.");

        db.ReportCards.Remove(card);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
