using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Attendance;
using NaijaPrimeSchool.Application.Attendance.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class SubjectAttendanceService(ApplicationDbContext db) : ISubjectAttendanceService
{
    private static IQueryable<SubjectAttendanceSessionDto> ProjectSession(IQueryable<SubjectAttendanceSession> q) =>
        q.Select(s => new SubjectAttendanceSessionDto
        {
            Id = s.Id,
            TimetableEntryId = s.TimetableEntryId,
            SchoolClassId = s.TimetableEntry!.SchoolClassId,
            SchoolClassName = s.TimetableEntry!.SchoolClass!.Name,
            SubjectId = s.TimetableEntry!.SubjectId,
            SubjectName = s.TimetableEntry!.Subject!.Name,
            SubjectCode = s.TimetableEntry!.Subject!.Code,
            TermId = s.TimetableEntry!.TermId,
            TermName = s.TimetableEntry!.Term!.TermType!.Name,
            SessionId = s.TimetableEntry!.Term!.SessionId,
            SessionName = s.TimetableEntry!.Term!.Session!.Name,
            WeekDayId = s.TimetableEntry!.WeekDayId,
            WeekDayName = s.TimetableEntry!.WeekDay!.Name,
            TimetablePeriodId = s.TimetableEntry!.TimetablePeriodId,
            PeriodName = s.TimetableEntry!.TimetablePeriod!.Name,
            PeriodStart = s.TimetableEntry!.TimetablePeriod!.StartTime,
            PeriodEnd = s.TimetableEntry!.TimetablePeriod!.EndTime,
            Date = s.Date,
            TakenById = s.TakenById,
            TakenByName = s.TakenBy == null
                ? null
                : (s.TakenBy.FirstName + " " + s.TakenBy.LastName).Trim(),
            TakenOn = s.TakenOn,
            IsSubmitted = s.IsSubmitted,
            SubmittedOn = s.SubmittedOn,
            Notes = s.Notes,
            PresentCount = s.Entries.Count(e => e.AttendanceStatus!.CountsAsPresent),
            AbsentCount = s.Entries.Count(e => !e.AttendanceStatus!.CountsAsPresent),
            TotalCount = s.Entries.Count,
        });

    public async Task<IReadOnlyList<SubjectAttendanceSessionDto>> ListAsync(SubjectSessionListFilter filter, CancellationToken ct = default)
    {
        var q = db.SubjectAttendanceSessions.AsQueryable();
        if (filter.TimetableEntryId.HasValue)
            q = q.Where(s => s.TimetableEntryId == filter.TimetableEntryId.Value);
        if (filter.SchoolClassId.HasValue)
            q = q.Where(s => s.TimetableEntry!.SchoolClassId == filter.SchoolClassId.Value);
        if (filter.TermId.HasValue)
            q = q.Where(s => s.TimetableEntry!.TermId == filter.TermId.Value);
        if (filter.SubjectId.HasValue)
            q = q.Where(s => s.TimetableEntry!.SubjectId == filter.SubjectId.Value);
        if (filter.FromDate.HasValue)
            q = q.Where(s => s.Date >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            q = q.Where(s => s.Date <= filter.ToDate.Value);

        return await ProjectSession(q.OrderByDescending(s => s.Date)
                .ThenBy(s => s.TimetableEntry!.TimetablePeriod!.DisplayOrder))
            .ToListAsync(ct);
    }

    public async Task<SubjectAttendanceSessionDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var session = await ProjectSession(db.SubjectAttendanceSessions.Where(s => s.Id == id))
            .FirstOrDefaultAsync(ct);
        if (session is null) return null;

        var entries = await ProjectEntries(db.SubjectAttendanceEntries.Where(e => e.SessionId == id))
            .ToListAsync(ct);

        return new SubjectAttendanceSessionDetailDto { Session = session, Entries = entries };
    }

    public async Task<SubjectAttendanceSessionDetailDto?> GetForEntryDateAsync(Guid timetableEntryId, DateOnly date, CancellationToken ct = default)
    {
        var sessionId = await db.SubjectAttendanceSessions
            .Where(s => s.TimetableEntryId == timetableEntryId && s.Date == date)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (sessionId == Guid.Empty) return null;
        return await GetByIdAsync(sessionId, ct);
    }

    public async Task<OperationResult<Guid>> OpenAsync(OpenSubjectSessionRequest request, CancellationToken ct = default)
    {
        var entry = await db.TimetableEntries
            .Include(e => e.SchoolClass)
            .Include(e => e.WeekDay)
            .Include(e => e.Term)
            .FirstOrDefaultAsync(e => e.Id == request.TimetableEntryId, ct);
        if (entry is null) return OperationResult<Guid>.Failure("Timetable entry not found.");

        // Sanity: the date should fall inside the entry's term and on the entry's weekday.
        if (request.Date < entry.Term!.StartDate || request.Date > entry.Term!.EndDate)
            return OperationResult<Guid>.Failure(
                "Selected date is outside the term this lesson belongs to.");

        var weekDayName = entry.WeekDay!.Name;
        var actualDayOfWeek = request.Date.DayOfWeek.ToString();
        if (!string.Equals(weekDayName, actualDayOfWeek, StringComparison.OrdinalIgnoreCase))
            return OperationResult<Guid>.Failure(
                $"Selected date is a {actualDayOfWeek} but this lesson runs on {weekDayName}.");

        var existing = await db.SubjectAttendanceSessions
            .FirstOrDefaultAsync(s => s.TimetableEntryId == request.TimetableEntryId && s.Date == request.Date, ct);
        if (existing is not null)
            return OperationResult<Guid>.Success(existing.Id);

        var session = new SubjectAttendanceSession
        {
            TimetableEntryId = request.TimetableEntryId,
            Date = request.Date,
            TakenById = request.TakenById ?? entry.TeacherId,
            TakenOn = request.TakenById.HasValue || entry.TeacherId.HasValue ? DateTimeOffset.UtcNow : null,
            IsSubmitted = false,
        };
        db.SubjectAttendanceSessions.Add(session);

        var defaultStatus = await db.AttendanceStatuses
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync(ct);
        if (defaultStatus is null)
            return OperationResult<Guid>.Failure("Attendance statuses are not seeded.");

        var enrolledStudents = await db.Enrolments
            .Where(e => e.SchoolClassId == entry.SchoolClassId
                        && e.EnrolledOn <= request.Date
                        && (e.WithdrawnOn == null || e.WithdrawnOn >= request.Date))
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        foreach (var studentId in enrolledStudents)
        {
            db.SubjectAttendanceEntries.Add(new SubjectAttendanceEntry
            {
                Session = session,
                StudentId = studentId,
                AttendanceStatusId = defaultStatus.Id,
            });
        }

        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(session.Id);
    }

    public async Task<OperationResult> SetEntryAsync(UpsertSubjectEntryRequest request, CancellationToken ct = default)
    {
        var session = await db.SubjectAttendanceSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null) return OperationResult.Failure("Session not found.");
        if (session.IsSubmitted) return OperationResult.Failure("Session is submitted. Reopen it first.");

        if (!await db.AttendanceStatuses.AnyAsync(s => s.Id == request.AttendanceStatusId, ct))
            return OperationResult.Failure("Attendance status not valid.");

        if (!await db.Students.AnyAsync(s => s.Id == request.StudentId, ct))
            return OperationResult.Failure("Student not found.");

        var entry = await db.SubjectAttendanceEntries
            .FirstOrDefaultAsync(e => e.SessionId == request.SessionId && e.StudentId == request.StudentId, ct);

        if (entry is null)
        {
            entry = new SubjectAttendanceEntry
            {
                SessionId = request.SessionId,
                StudentId = request.StudentId,
                AttendanceStatusId = request.AttendanceStatusId,
                Remarks = request.Remarks,
            };
            db.SubjectAttendanceEntries.Add(entry);
        }
        else
        {
            entry.AttendanceStatusId = request.AttendanceStatusId;
            entry.Remarks = request.Remarks;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> BulkSetAsync(BulkSetSubjectAttendanceRequest request, CancellationToken ct = default)
    {
        var session = await db.SubjectAttendanceSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null) return OperationResult.Failure("Session not found.");
        if (session.IsSubmitted) return OperationResult.Failure("Session is submitted. Reopen it first.");

        var validStatusIds = await db.AttendanceStatuses.Select(s => s.Id).ToListAsync(ct);
        var statusSet = validStatusIds.ToHashSet();

        var existing = await db.SubjectAttendanceEntries
            .Where(e => e.SessionId == request.SessionId)
            .ToDictionaryAsync(e => e.StudentId, ct);

        foreach (var item in request.Entries)
        {
            if (!statusSet.Contains(item.AttendanceStatusId))
                return OperationResult.Failure("One or more attendance statuses are invalid.");

            if (existing.TryGetValue(item.StudentId, out var entry))
            {
                entry.AttendanceStatusId = item.AttendanceStatusId;
                entry.Remarks = item.Remarks;
            }
            else
            {
                db.SubjectAttendanceEntries.Add(new SubjectAttendanceEntry
                {
                    SessionId = request.SessionId,
                    StudentId = item.StudentId,
                    AttendanceStatusId = item.AttendanceStatusId,
                    Remarks = item.Remarks,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SubmitAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.SubjectAttendanceSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null) return OperationResult.Failure("Session not found.");
        if (session.IsSubmitted) return OperationResult.Success();

        session.IsSubmitted = true;
        session.SubmittedOn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ReopenAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.SubjectAttendanceSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null) return OperationResult.Failure("Session not found.");

        session.IsSubmitted = false;
        session.SubmittedOn = null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.SubjectAttendanceSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null) return OperationResult.Failure("Session not found.");

        if (session.IsSubmitted)
            return OperationResult.Failure("Submitted sessions cannot be deleted. Reopen first.");

        db.SubjectAttendanceSessions.Remove(session);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    private static IQueryable<SubjectAttendanceEntryDto> ProjectEntries(IQueryable<SubjectAttendanceEntry> q) =>
        q.OrderBy(e => e.Student!.FirstName).ThenBy(e => e.Student!.LastName)
            .Select(e => new SubjectAttendanceEntryDto
            {
                Id = e.Id,
                SessionId = e.SessionId,
                StudentId = e.StudentId,
                StudentName = (e.Student!.FirstName + " " + e.Student!.LastName).Trim(),
                StudentAdmissionNumber = e.Student!.AdmissionNumber,
                AttendanceStatusId = e.AttendanceStatusId,
                AttendanceStatusName = e.AttendanceStatus!.Name,
                AttendanceStatusCode = e.AttendanceStatus!.Code,
                CountsAsPresent = e.AttendanceStatus!.CountsAsPresent,
                Remarks = e.Remarks,
            });
}
