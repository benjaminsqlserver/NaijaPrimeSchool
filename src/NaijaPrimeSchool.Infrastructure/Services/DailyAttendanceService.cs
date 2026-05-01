using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Attendance;
using NaijaPrimeSchool.Application.Attendance.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class DailyAttendanceService(ApplicationDbContext db) : IDailyAttendanceService
{
    private static IQueryable<DailyAttendanceRegisterDto> ProjectRegister(IQueryable<DailyAttendanceRegister> q) =>
        q.Select(r => new DailyAttendanceRegisterDto
        {
            Id = r.Id,
            SchoolClassId = r.SchoolClassId,
            SchoolClassName = r.SchoolClass!.Name,
            SessionId = r.SchoolClass!.SessionId,
            SessionName = r.SchoolClass!.Session!.Name,
            TermId = r.TermId,
            TermName = r.Term!.TermType!.Name,
            Date = r.Date,
            TakenById = r.TakenById,
            TakenByName = r.TakenBy == null
                ? null
                : (r.TakenBy.FirstName + " " + r.TakenBy.LastName).Trim(),
            TakenOn = r.TakenOn,
            IsSubmitted = r.IsSubmitted,
            SubmittedOn = r.SubmittedOn,
            Notes = r.Notes,
            PresentCount = r.Entries.Count(e => e.AttendanceStatus!.CountsAsPresent),
            AbsentCount = r.Entries.Count(e => !e.AttendanceStatus!.CountsAsPresent),
            LateCount = r.Entries.Count(e => e.AttendanceStatus!.Code == "L"),
            TotalCount = r.Entries.Count,
        });

    public async Task<IReadOnlyList<DailyAttendanceRegisterDto>> ListAsync(DailyRegisterListFilter filter, CancellationToken ct = default)
    {
        var q = db.DailyAttendanceRegisters.AsQueryable();

        if (filter.SchoolClassId.HasValue)
            q = q.Where(r => r.SchoolClassId == filter.SchoolClassId.Value);
        if (filter.TermId.HasValue)
            q = q.Where(r => r.TermId == filter.TermId.Value);
        if (filter.SessionId.HasValue)
            q = q.Where(r => r.SchoolClass!.SessionId == filter.SessionId.Value);
        if (filter.FromDate.HasValue)
            q = q.Where(r => r.Date >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            q = q.Where(r => r.Date <= filter.ToDate.Value);
        if (filter.IsSubmitted.HasValue)
            q = q.Where(r => r.IsSubmitted == filter.IsSubmitted.Value);

        return await ProjectRegister(q.OrderByDescending(r => r.Date))
            .ToListAsync(ct);
    }

    public async Task<DailyAttendanceRegisterDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var register = await ProjectRegister(db.DailyAttendanceRegisters.Where(r => r.Id == id))
            .FirstOrDefaultAsync(ct);
        if (register is null) return null;

        var entries = await ProjectEntries(db.DailyAttendanceEntries.Where(e => e.RegisterId == id))
            .ToListAsync(ct);

        return new DailyAttendanceRegisterDetailDto { Register = register, Entries = entries };
    }

    public async Task<DailyAttendanceRegisterDetailDto?> GetForClassDateAsync(Guid schoolClassId, DateOnly date, CancellationToken ct = default)
    {
        var register = await db.DailyAttendanceRegisters
            .Where(r => r.SchoolClassId == schoolClassId && r.Date == date)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (register == Guid.Empty) return null;
        return await GetByIdAsync(register, ct);
    }

    public async Task<OperationResult<Guid>> OpenAsync(OpenDailyRegisterRequest request, CancellationToken ct = default)
    {
        var schoolClass = await db.SchoolClasses
            .Include(c => c.Session)
            .FirstOrDefaultAsync(c => c.Id == request.SchoolClassId, ct);
        if (schoolClass is null)
            return OperationResult<Guid>.Failure("Class not found.");

        var term = await db.Terms
            .Where(t => t.SessionId == schoolClass.SessionId
                        && t.StartDate <= request.Date
                        && t.EndDate >= request.Date)
            .FirstOrDefaultAsync(ct);

        if (term is null)
        {
            term = await db.Terms
                .Where(t => t.SessionId == schoolClass.SessionId && t.IsCurrent)
                .FirstOrDefaultAsync(ct);
        }

        if (term is null)
            return OperationResult<Guid>.Failure(
                "No term covers this date and no current term is set for the class's session.");

        var existing = await db.DailyAttendanceRegisters
            .FirstOrDefaultAsync(r => r.SchoolClassId == request.SchoolClassId && r.Date == request.Date, ct);
        if (existing is not null)
            return OperationResult<Guid>.Success(existing.Id);

        var register = new DailyAttendanceRegister
        {
            SchoolClassId = request.SchoolClassId,
            TermId = term.Id,
            Date = request.Date,
            TakenById = request.TakenById,
            TakenOn = request.TakenById.HasValue ? DateTimeOffset.UtcNow : null,
            IsSubmitted = false,
        };
        db.DailyAttendanceRegisters.Add(register);

        var defaultStatus = await db.AttendanceStatuses
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync(ct);
        if (defaultStatus is null)
            return OperationResult<Guid>.Failure("Attendance statuses are not seeded.");

        var enrolledStudents = await db.Enrolments
            .Where(e => e.SchoolClassId == request.SchoolClassId
                        && e.EnrolledOn <= request.Date
                        && (e.WithdrawnOn == null || e.WithdrawnOn >= request.Date))
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        foreach (var studentId in enrolledStudents)
        {
            db.DailyAttendanceEntries.Add(new DailyAttendanceEntry
            {
                Register = register,
                StudentId = studentId,
                AttendanceStatusId = defaultStatus.Id,
            });
        }

        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(register.Id);
    }

    public async Task<OperationResult> SetEntryAsync(UpsertDailyEntryRequest request, CancellationToken ct = default)
    {
        var register = await db.DailyAttendanceRegisters.FirstOrDefaultAsync(r => r.Id == request.RegisterId, ct);
        if (register is null) return OperationResult.Failure("Register not found.");
        if (register.IsSubmitted) return OperationResult.Failure("Register is submitted. Reopen it first.");

        if (!await db.AttendanceStatuses.AnyAsync(s => s.Id == request.AttendanceStatusId, ct))
            return OperationResult.Failure("Attendance status not valid.");

        if (!await db.Students.AnyAsync(s => s.Id == request.StudentId, ct))
            return OperationResult.Failure("Student not found.");

        var entry = await db.DailyAttendanceEntries
            .FirstOrDefaultAsync(e => e.RegisterId == request.RegisterId && e.StudentId == request.StudentId, ct);

        if (entry is null)
        {
            entry = new DailyAttendanceEntry
            {
                RegisterId = request.RegisterId,
                StudentId = request.StudentId,
                AttendanceStatusId = request.AttendanceStatusId,
                ArrivalTime = request.ArrivalTime,
                Remarks = request.Remarks,
            };
            db.DailyAttendanceEntries.Add(entry);
        }
        else
        {
            entry.AttendanceStatusId = request.AttendanceStatusId;
            entry.ArrivalTime = request.ArrivalTime;
            entry.Remarks = request.Remarks;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> BulkSetAsync(BulkSetDailyAttendanceRequest request, CancellationToken ct = default)
    {
        var register = await db.DailyAttendanceRegisters.FirstOrDefaultAsync(r => r.Id == request.RegisterId, ct);
        if (register is null) return OperationResult.Failure("Register not found.");
        if (register.IsSubmitted) return OperationResult.Failure("Register is submitted. Reopen it first.");

        var validStatusIds = await db.AttendanceStatuses.Select(s => s.Id).ToListAsync(ct);
        var statusSet = validStatusIds.ToHashSet();

        var existing = await db.DailyAttendanceEntries
            .Where(e => e.RegisterId == request.RegisterId)
            .ToDictionaryAsync(e => e.StudentId, ct);

        foreach (var item in request.Entries)
        {
            if (!statusSet.Contains(item.AttendanceStatusId))
                return OperationResult.Failure("One or more attendance statuses are invalid.");

            if (existing.TryGetValue(item.StudentId, out var entry))
            {
                entry.AttendanceStatusId = item.AttendanceStatusId;
                entry.ArrivalTime = item.ArrivalTime;
                entry.Remarks = item.Remarks;
            }
            else
            {
                db.DailyAttendanceEntries.Add(new DailyAttendanceEntry
                {
                    RegisterId = request.RegisterId,
                    StudentId = item.StudentId,
                    AttendanceStatusId = item.AttendanceStatusId,
                    ArrivalTime = item.ArrivalTime,
                    Remarks = item.Remarks,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SubmitAsync(Guid registerId, CancellationToken ct = default)
    {
        var register = await db.DailyAttendanceRegisters.FirstOrDefaultAsync(r => r.Id == registerId, ct);
        if (register is null) return OperationResult.Failure("Register not found.");
        if (register.IsSubmitted) return OperationResult.Success();

        register.IsSubmitted = true;
        register.SubmittedOn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ReopenAsync(Guid registerId, CancellationToken ct = default)
    {
        var register = await db.DailyAttendanceRegisters.FirstOrDefaultAsync(r => r.Id == registerId, ct);
        if (register is null) return OperationResult.Failure("Register not found.");

        register.IsSubmitted = false;
        register.SubmittedOn = null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid registerId, CancellationToken ct = default)
    {
        var register = await db.DailyAttendanceRegisters.FirstOrDefaultAsync(r => r.Id == registerId, ct);
        if (register is null) return OperationResult.Failure("Register not found.");

        if (register.IsSubmitted)
            return OperationResult.Failure("Submitted registers cannot be deleted. Reopen first.");

        db.DailyAttendanceRegisters.Remove(register);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<StudentAttendanceSummaryDto> GetStudentSummaryAsync(StudentAttendanceSummaryFilter filter, CancellationToken ct = default)
    {
        var student = await db.Students.Where(s => s.Id == filter.StudentId)
            .Select(s => new { s.FirstName, s.LastName, s.AdmissionNumber })
            .FirstOrDefaultAsync(ct);

        var query = db.DailyAttendanceEntries
            .Where(e => e.StudentId == filter.StudentId);

        if (filter.TermId.HasValue)
            query = query.Where(e => e.Register!.TermId == filter.TermId.Value);
        if (filter.SessionId.HasValue)
            query = query.Where(e => e.Register!.Term!.SessionId == filter.SessionId.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(e => e.Register!.Date >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(e => e.Register!.Date <= filter.ToDate.Value);

        var counts = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                DaysCounted = g.Count(),
                DaysPresent = g.Count(e => e.AttendanceStatus!.CountsAsPresent),
                DaysLate = g.Count(e => e.AttendanceStatus!.Code == "L"),
                DaysAbsent = g.Count(e => e.AttendanceStatus!.Code == "A"),
                DaysExcused = g.Count(e => e.AttendanceStatus!.Code == "E"),
            })
            .FirstOrDefaultAsync(ct);

        return new StudentAttendanceSummaryDto
        {
            StudentId = filter.StudentId,
            StudentName = student is null
                ? string.Empty
                : (student.FirstName + " " + student.LastName).Trim(),
            StudentAdmissionNumber = student?.AdmissionNumber ?? string.Empty,
            DaysCounted = counts?.DaysCounted ?? 0,
            DaysPresent = counts?.DaysPresent ?? 0,
            DaysLate = counts?.DaysLate ?? 0,
            DaysAbsent = counts?.DaysAbsent ?? 0,
            DaysExcused = counts?.DaysExcused ?? 0,
        };
    }

    public async Task<ClassAttendanceSummaryDto> GetClassSummaryAsync(Guid schoolClassId, Guid? termId, CancellationToken ct = default)
    {
        var schoolClass = await db.SchoolClasses
            .Where(c => c.Id == schoolClassId)
            .Select(c => new { c.Name })
            .FirstOrDefaultAsync(ct);

        var enrolmentQuery = db.Enrolments
            .Where(e => e.SchoolClassId == schoolClassId);

        var students = await enrolmentQuery
            .Select(e => new
            {
                e.StudentId,
                StudentName = e.Student!.FirstName + " " + e.Student!.LastName,
                e.Student!.AdmissionNumber,
            })
            .Distinct()
            .ToListAsync(ct);

        var summaries = new List<StudentAttendanceSummaryDto>();
        foreach (var s in students)
        {
            var summary = await GetStudentSummaryAsync(new StudentAttendanceSummaryFilter
            {
                StudentId = s.StudentId,
                TermId = termId,
            }, ct);
            summary.StudentName = s.StudentName.Trim();
            summary.StudentAdmissionNumber = s.AdmissionNumber;
            summaries.Add(summary);
        }

        return new ClassAttendanceSummaryDto
        {
            SchoolClassId = schoolClassId,
            SchoolClassName = schoolClass?.Name ?? string.Empty,
            Students = summaries.OrderBy(s => s.StudentName).ToList(),
        };
    }

    private static IQueryable<DailyAttendanceEntryDto> ProjectEntries(IQueryable<DailyAttendanceEntry> q) =>
        q.OrderBy(e => e.Student!.FirstName).ThenBy(e => e.Student!.LastName)
            .Select(e => new DailyAttendanceEntryDto
            {
                Id = e.Id,
                RegisterId = e.RegisterId,
                StudentId = e.StudentId,
                StudentName = (e.Student!.FirstName + " " + e.Student!.LastName).Trim(),
                StudentAdmissionNumber = e.Student!.AdmissionNumber,
                AttendanceStatusId = e.AttendanceStatusId,
                AttendanceStatusName = e.AttendanceStatus!.Name,
                AttendanceStatusCode = e.AttendanceStatus!.Code,
                CountsAsPresent = e.AttendanceStatus!.CountsAsPresent,
                ArrivalTime = e.ArrivalTime,
                Remarks = e.Remarks,
            });
}
