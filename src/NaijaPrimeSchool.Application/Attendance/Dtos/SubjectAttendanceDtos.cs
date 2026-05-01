using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Attendance.Dtos;

public class SubjectAttendanceSessionDto
{
    public Guid Id { get; set; }

    public Guid TimetableEntryId { get; set; }

    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;

    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;

    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;

    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    public Guid WeekDayId { get; set; }
    public string WeekDayName { get; set; } = string.Empty;

    public Guid TimetablePeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public TimeOnly PeriodStart { get; set; }
    public TimeOnly PeriodEnd { get; set; }

    public DateOnly Date { get; set; }

    public Guid? TakenById { get; set; }
    public string? TakenByName { get; set; }
    public DateTimeOffset? TakenOn { get; set; }

    public bool IsSubmitted { get; set; }
    public DateTimeOffset? SubmittedOn { get; set; }

    public string? Notes { get; set; }

    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int TotalCount { get; set; }
}

public class SubjectAttendanceEntryDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;

    public Guid AttendanceStatusId { get; set; }
    public string AttendanceStatusName { get; set; } = string.Empty;
    public string AttendanceStatusCode { get; set; } = string.Empty;
    public bool CountsAsPresent { get; set; }

    public string? Remarks { get; set; }
}

public class SubjectAttendanceSessionDetailDto
{
    public SubjectAttendanceSessionDto Session { get; set; } = new();
    public List<SubjectAttendanceEntryDto> Entries { get; set; } = [];
}

public class OpenSubjectSessionRequest
{
    [Required] public Guid TimetableEntryId { get; set; }
    [Required] public DateOnly Date { get; set; }
    public Guid? TakenById { get; set; }
}

public class UpsertSubjectEntryRequest
{
    public Guid? Id { get; set; }
    [Required] public Guid SessionId { get; set; }
    [Required] public Guid StudentId { get; set; }
    [Required] public Guid AttendanceStatusId { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }
}

public class BulkSetSubjectAttendanceRequest
{
    [Required] public Guid SessionId { get; set; }
    public List<UpsertSubjectEntryRequest> Entries { get; set; } = [];
}

public class SubjectSessionListFilter
{
    public Guid? TimetableEntryId { get; set; }
    public Guid? SchoolClassId { get; set; }
    public Guid? TermId { get; set; }
    public Guid? SubjectId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
