namespace NaijaPrimeSchool.Application.Portals.Dtos;

public class WardSummaryDto
{
    public Guid StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? PhotoUrl { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public bool IsPrimaryContact { get; set; }
    public bool CanPickUp { get; set; }

    public Guid? CurrentSchoolClassId { get; set; }
    public string? CurrentSchoolClassName { get; set; }
    public string? CurrentEnrolmentStatus { get; set; }

    public decimal OutstandingBalance { get; set; }
    public int PublishedReportCardCount { get; set; }
    public double? LatestAttendancePercentage { get; set; }
}

public class ParentDashboardDto
{
    public Guid ParentId { get; set; }
    public string ParentFullName { get; set; } = string.Empty;
    public string? ParentPhone { get; set; }
    public string? ParentEmail { get; set; }
    public IReadOnlyList<WardSummaryDto> Wards { get; set; } = [];
    public int UnreadAnnouncementCount { get; set; }
}

public class StudentDashboardDto
{
    public Guid StudentId { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    public Guid? CurrentSchoolClassId { get; set; }
    public string? CurrentSchoolClassName { get; set; }
    public Guid? CurrentTermId { get; set; }
    public string? CurrentTermName { get; set; }
    public string? CurrentSessionName { get; set; }

    public decimal OutstandingBalance { get; set; }
    public decimal LifetimeInvoiced { get; set; }
    public decimal LifetimePaid { get; set; }

    public int PublishedReportCardCount { get; set; }
    public double? LatestAttendancePercentage { get; set; }
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int DaysLate { get; set; }
    public int DaysCounted { get; set; }

    public int UnreadAnnouncementCount { get; set; }

    public IReadOnlyList<UpcomingLessonDto> TodayLessons { get; set; } = [];
}

public class UpcomingLessonDto
{
    public Guid TimetableEntryId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public TimeOnly? StartsAt { get; set; }
    public TimeOnly? EndsAt { get; set; }
    public string? Room { get; set; }
    public string? TeacherName { get; set; }
}
