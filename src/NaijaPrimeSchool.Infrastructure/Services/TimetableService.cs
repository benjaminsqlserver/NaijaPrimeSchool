using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Academics;
using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class TimetableService(ApplicationDbContext db) : ITimetableService
{
    // ----- Periods -----

    public async Task<IReadOnlyList<TimetablePeriodDto>> ListPeriodsAsync(CancellationToken ct = default) =>
        await db.TimetablePeriods
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.StartTime)
            .Select(p => new TimetablePeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                StartTime = p.StartTime,
                EndTime = p.EndTime,
                DisplayOrder = p.DisplayOrder,
                IsBreak = p.IsBreak,
            })
            .ToListAsync(ct);

    public Task<TimetablePeriodDto?> GetPeriodByIdAsync(Guid id, CancellationToken ct = default) =>
        db.TimetablePeriods
            .Where(p => p.Id == id)
            .Select(p => new TimetablePeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                StartTime = p.StartTime,
                EndTime = p.EndTime,
                DisplayOrder = p.DisplayOrder,
                IsBreak = p.IsBreak,
            })
            .FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreatePeriodAsync(CreateTimetablePeriodRequest request, CancellationToken ct = default)
    {
        if (request.EndTime <= request.StartTime)
            return OperationResult<Guid>.Failure("End time must be after start time.");

        var entity = new TimetablePeriod
        {
            Name = request.Name.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            DisplayOrder = request.DisplayOrder,
            IsBreak = request.IsBreak,
        };
        db.TimetablePeriods.Add(entity);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(entity.Id);
    }

    public async Task<OperationResult> UpdatePeriodAsync(UpdateTimetablePeriodRequest request, CancellationToken ct = default)
    {
        var entity = await db.TimetablePeriods.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (entity is null) return OperationResult.Failure("Period not found.");

        if (request.EndTime <= request.StartTime)
            return OperationResult.Failure("End time must be after start time.");

        entity.Name = request.Name.Trim();
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsBreak = request.IsBreak;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeletePeriodAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.TimetablePeriods
            .Include(p => p.TimetableEntries)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return OperationResult.Failure("Period not found.");

        if (entity.TimetableEntries.Count > 0)
            return OperationResult.Failure("Cannot delete a period that is in use on a timetable.");

        db.TimetablePeriods.Remove(entity);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    // ----- Entries -----

    private static IQueryable<TimetableEntryDto> ProjectEntries(IQueryable<TimetableEntry> q) =>
        q.Select(e => new TimetableEntryDto
        {
            Id = e.Id,
            TermId = e.TermId,
            SchoolClassId = e.SchoolClassId,
            SchoolClassName = e.SchoolClass!.Name,
            SubjectId = e.SubjectId,
            SubjectName = e.Subject!.Name,
            SubjectCode = e.Subject!.Code,
            WeekDayId = e.WeekDayId,
            WeekDayName = e.WeekDay!.Name,
            WeekDayOrder = e.WeekDay!.DisplayOrder,
            TimetablePeriodId = e.TimetablePeriodId,
            PeriodName = e.TimetablePeriod!.Name,
            PeriodStart = e.TimetablePeriod!.StartTime,
            PeriodEnd = e.TimetablePeriod!.EndTime,
            PeriodOrder = e.TimetablePeriod!.DisplayOrder,
            TeacherId = e.TeacherId,
            TeacherName = e.Teacher == null
                ? null
                : (e.Teacher.FirstName + " " + e.Teacher.LastName).Trim(),
            Room = e.Room,
            Notes = e.Notes,
        });

    public async Task<IReadOnlyList<TimetableEntryDto>> ListEntriesAsync(TimetableQuery query, CancellationToken ct = default) =>
        await ProjectEntries(db.TimetableEntries
                .Where(e => e.TermId == query.TermId && e.SchoolClassId == query.SchoolClassId))
            .OrderBy(e => e.WeekDayOrder)
            .ThenBy(e => e.PeriodOrder)
            .ToListAsync(ct);

    public Task<TimetableEntryDto?> GetEntryByIdAsync(Guid id, CancellationToken ct = default) =>
        ProjectEntries(db.TimetableEntries.Where(e => e.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> UpsertEntryAsync(UpsertTimetableEntryRequest request, CancellationToken ct = default)
    {
        if (!await db.Terms.AnyAsync(t => t.Id == request.TermId, ct))
            return OperationResult<Guid>.Failure("Term not found.");
        if (!await db.SchoolClasses.AnyAsync(c => c.Id == request.SchoolClassId, ct))
            return OperationResult<Guid>.Failure("Class not found.");
        if (!await db.Subjects.AnyAsync(s => s.Id == request.SubjectId, ct))
            return OperationResult<Guid>.Failure("Subject not found.");
        if (!await db.WeekDays.AnyAsync(d => d.Id == request.WeekDayId, ct))
            return OperationResult<Guid>.Failure("Week day not found.");
        if (!await db.TimetablePeriods.AnyAsync(p => p.Id == request.TimetablePeriodId, ct))
            return OperationResult<Guid>.Failure("Period not found.");

        var existingId = await db.TimetableEntries
            .Where(e => e.TermId == request.TermId
                        && e.SchoolClassId == request.SchoolClassId
                        && e.WeekDayId == request.WeekDayId
                        && e.TimetablePeriodId == request.TimetablePeriodId)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct);

        // Treat (term, class, day, period) as the natural key, regardless of the supplied Id
        var entity = existingId.HasValue
            ? await db.TimetableEntries.FirstAsync(e => e.Id == existingId.Value, ct)
            : new TimetableEntry();

        entity.TermId = request.TermId;
        entity.SchoolClassId = request.SchoolClassId;
        entity.SubjectId = request.SubjectId;
        entity.WeekDayId = request.WeekDayId;
        entity.TimetablePeriodId = request.TimetablePeriodId;
        entity.TeacherId = request.TeacherId;
        entity.Room = request.Room;
        entity.Notes = request.Notes;

        if (!existingId.HasValue) db.TimetableEntries.Add(entity);

        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(entity.Id);
    }

    public async Task<OperationResult> SoftDeleteEntryAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.TimetableEntries.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return OperationResult.Failure("Timetable entry not found.");
        db.TimetableEntries.Remove(entity);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
