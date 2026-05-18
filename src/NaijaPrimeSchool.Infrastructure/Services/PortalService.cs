using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications;
using NaijaPrimeSchool.Application.Finance;
using NaijaPrimeSchool.Application.Portals;
using NaijaPrimeSchool.Application.Portals.Dtos;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class PortalService(
    ApplicationDbContext db,
    ICurrentUser currentUser,
    IInvoiceService invoices,
    IAnnouncementService announcements) : IPortalService
{
    public async Task<Guid?> ResolveParentIdForCurrentUserAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return null;
        return await db.Parents
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid?> ResolveStudentIdForCurrentUserAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId) return null;
        return await db.Students
            .Where(s => s.UserId == userId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ParentDashboardDto?> GetParentDashboardAsync(Guid parentId, CancellationToken ct = default)
    {
        var parent = await db.Parents
            .Include(p => p.StudentLinks).ThenInclude(sl => sl.Student)
            .Include(p => p.StudentLinks).ThenInclude(sl => sl.Relationship)
            .FirstOrDefaultAsync(p => p.Id == parentId, ct);
        if (parent is null) return null;

        var wards = new List<WardSummaryDto>();
        foreach (var link in parent.StudentLinks)
        {
            if (link.Student is null) continue;
            wards.Add(await BuildWardSummaryAsync(link.Student.Id, link.Relationship?.Name ?? "Guardian",
                link.IsPrimaryContact, link.CanPickUp, ct));
        }

        var unread = await announcements.CountUnreadForCurrentUserAsync(ct);

        return new ParentDashboardDto
        {
            ParentId = parent.Id,
            ParentFullName = parent.FullName,
            ParentPhone = parent.PrimaryPhone,
            ParentEmail = parent.Email,
            Wards = wards
                .OrderByDescending(w => w.IsPrimaryContact)
                .ThenBy(w => w.FullName)
                .ToList(),
            UnreadAnnouncementCount = unread,
        };
    }

    public async Task<StudentDashboardDto?> GetStudentDashboardAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await db.Students
            .Include(s => s.Enrolments).ThenInclude(e => e.SchoolClass).ThenInclude(c => c!.Session)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return null;

        var currentEnrolment = student.Enrolments
            .Where(e => e.WithdrawnOn == null)
            .OrderByDescending(e => e.EnrolledOn)
            .FirstOrDefault();

        var currentTerm = await db.Terms
            .Include(t => t.Session)
            .Include(t => t.TermType)
            .Where(t => t.IsCurrent)
            .FirstOrDefaultAsync(ct);

        var ledger = await invoices.GetStudentLedgerAsync(studentId, null, ct);

        var publishedCards = await db.ReportCards
            .Where(r => r.StudentId == studentId && r.IsPublished)
            .CountAsync(ct);

        var attendance = await ComputeAttendanceForCurrentTermAsync(studentId, currentEnrolment?.SchoolClassId, currentTerm?.Id, ct);

        var lessons = currentEnrolment is null || currentTerm is null
            ? Array.Empty<UpcomingLessonDto>()
            : await GetTodaysLessonsAsync(currentEnrolment.SchoolClassId, currentTerm.Id, ct);

        return new StudentDashboardDto
        {
            StudentId = student.Id,
            AdmissionNumber = student.AdmissionNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            PhotoUrl = student.PhotoUrl,
            CurrentSchoolClassId = currentEnrolment?.SchoolClassId,
            CurrentSchoolClassName = currentEnrolment?.SchoolClass?.Name,
            CurrentSessionName = currentEnrolment?.SchoolClass?.Session?.Name,
            CurrentTermId = currentTerm?.Id,
            CurrentTermName = currentTerm?.TermType?.Name,
            OutstandingBalance = ledger?.OutstandingBalance ?? 0,
            LifetimeInvoiced = ledger?.TotalInvoiced ?? 0,
            LifetimePaid = ledger?.TotalPaid ?? 0,
            PublishedReportCardCount = publishedCards,
            LatestAttendancePercentage = attendance.Percentage,
            DaysPresent = attendance.Present,
            DaysAbsent = attendance.Absent,
            DaysLate = attendance.Late,
            DaysCounted = attendance.Counted,
            UnreadAnnouncementCount = await announcements.CountUnreadForCurrentUserAsync(ct),
            TodayLessons = lessons,
        };
    }

    public async Task<bool> CurrentUserCanViewStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        if (currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.HeadTeacher))
            return true;
        if (currentUser.UserId is not { } userId) return false;

        // Student views their own record
        var isSelf = await db.Students.AnyAsync(s => s.Id == studentId && s.UserId == userId, ct);
        if (isSelf) return true;

        // Parent views any linked ward
        return await db.StudentParents
            .AnyAsync(sp => sp.StudentId == studentId && sp.Parent!.UserId == userId, ct);
    }

    private async Task<WardSummaryDto> BuildWardSummaryAsync(
        Guid studentId, string relationship, bool isPrimary, bool canPickUp, CancellationToken ct)
    {
        var student = await db.Students
            .Include(s => s.Enrolments).ThenInclude(e => e.SchoolClass)
            .Include(s => s.Enrolments).ThenInclude(e => e.EnrolmentStatus)
            .FirstAsync(s => s.Id == studentId, ct);

        var enrolment = student.Enrolments
            .Where(e => e.WithdrawnOn == null)
            .OrderByDescending(e => e.EnrolledOn)
            .FirstOrDefault();

        var ledger = await invoices.GetStudentLedgerAsync(studentId, null, ct);
        var publishedCards = await db.ReportCards
            .CountAsync(r => r.StudentId == studentId && r.IsPublished, ct);

        var currentTerm = await db.Terms
            .Where(t => t.IsCurrent)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);
        var attendance = await ComputeAttendanceForCurrentTermAsync(studentId, enrolment?.SchoolClassId, currentTerm, ct);

        return new WardSummaryDto
        {
            StudentId = studentId,
            AdmissionNumber = student.AdmissionNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            PhotoUrl = student.PhotoUrl,
            Relationship = relationship,
            IsPrimaryContact = isPrimary,
            CanPickUp = canPickUp,
            CurrentSchoolClassId = enrolment?.SchoolClassId,
            CurrentSchoolClassName = enrolment?.SchoolClass?.Name,
            CurrentEnrolmentStatus = enrolment?.EnrolmentStatus?.Name,
            OutstandingBalance = ledger?.OutstandingBalance ?? 0,
            PublishedReportCardCount = publishedCards,
            LatestAttendancePercentage = attendance.Percentage,
        };
    }

    private async Task<(int Counted, int Present, int Late, int Absent, double? Percentage)>
        ComputeAttendanceForCurrentTermAsync(Guid studentId, Guid? classId, Guid? termId, CancellationToken ct)
    {
        if (classId is null || termId is null) return (0, 0, 0, 0, null);

        var entries = await db.DailyAttendanceEntries
            .Include(e => e.AttendanceStatus)
            .Include(e => e.Register)
            .Where(e => e.StudentId == studentId
                && e.Register!.SchoolClassId == classId
                && e.Register.TermId == termId)
            .ToListAsync(ct);

        var counted = entries.Count;
        if (counted == 0) return (0, 0, 0, 0, null);

        var present = entries.Count(e => e.AttendanceStatus?.Code == "P");
        var late = entries.Count(e => e.AttendanceStatus?.Code == "L");
        var absent = entries.Count(e => e.AttendanceStatus?.Code == "A");
        var countsAsPresent = entries.Count(e => e.AttendanceStatus?.CountsAsPresent == true);
        var pct = counted == 0 ? (double?)null : Math.Round(countsAsPresent * 100.0 / counted, 1);

        return (counted, present, late, absent, pct);
    }

    private async Task<IReadOnlyList<UpcomingLessonDto>> GetTodaysLessonsAsync(
        Guid classId, Guid termId, CancellationToken ct)
    {
        // WeekDay.DisplayOrder is 1 = Monday .. 5 = Friday (the seeded set).
        // .NET DayOfWeek is Sunday = 0, Monday = 1, ..., Saturday = 6.
        // Sunday and Saturday simply produce an empty list.
        var dow = DateTime.UtcNow.DayOfWeek;
        var displayOrder = dow == DayOfWeek.Sunday ? 0 : (int)dow;
        var entries = await db.TimetableEntries
            .Include(t => t.Subject)
            .Include(t => t.TimetablePeriod)
            .Include(t => t.WeekDay)
            .Include(t => t.Teacher)
            .Where(t => t.SchoolClassId == classId
                && t.TermId == termId
                && t.WeekDay!.DisplayOrder == displayOrder)
            .OrderBy(t => t.TimetablePeriod!.StartTime)
            .ToListAsync(ct);

        return entries.Select(t => new UpcomingLessonDto
        {
            TimetableEntryId = t.Id,
            SubjectName = t.Subject?.Name ?? string.Empty,
            SubjectCode = t.Subject?.Code ?? string.Empty,
            PeriodName = t.TimetablePeriod?.Name ?? string.Empty,
            StartsAt = t.TimetablePeriod?.StartTime,
            EndsAt = t.TimetablePeriod?.EndTime,
            Room = t.Room,
            TeacherName = t.Teacher is null ? null : $"{t.Teacher.FirstName} {t.Teacher.LastName}".Trim(),
        }).ToList();
    }
}
