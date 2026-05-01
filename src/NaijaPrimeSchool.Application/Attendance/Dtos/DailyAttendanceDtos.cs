using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Attendance.Dtos;

public class DailyAttendanceRegisterDto
{
    public Guid Id { get; set; }

    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;

    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public Guid? TakenById { get; set; }
    public string? TakenByName { get; set; }
    public DateTimeOffset? TakenOn { get; set; }

    public bool IsSubmitted { get; set; }
    public DateTimeOffset? SubmittedOn { get; set; }

    public string? Notes { get; set; }

    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int TotalCount { get; set; }
}

public class DailyAttendanceEntryDto
{
    public Guid Id { get; set; }
    public Guid RegisterId { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;

    public Guid AttendanceStatusId { get; set; }
    public string AttendanceStatusName { get; set; } = string.Empty;
    public string AttendanceStatusCode { get; set; } = string.Empty;
    public bool CountsAsPresent { get; set; }

    public TimeOnly? ArrivalTime { get; set; }
    public string? Remarks { get; set; }
}

public class DailyAttendanceRegisterDetailDto
{
    public DailyAttendanceRegisterDto Register { get; set; } = new();
    public List<DailyAttendanceEntryDto> Entries { get; set; } = [];
}

public class OpenDailyRegisterRequest
{
    [Required] public Guid SchoolClassId { get; set; }
    [Required] public DateOnly Date { get; set; }
    public Guid? TakenById { get; set; }
}

public class UpsertDailyEntryRequest
{
    public Guid? Id { get; set; }
    [Required] public Guid RegisterId { get; set; }
    [Required] public Guid StudentId { get; set; }
    [Required] public Guid AttendanceStatusId { get; set; }
    public TimeOnly? ArrivalTime { get; set; }

    [StringLength(300)]
    public string? Remarks { get; set; }
}

public class BulkSetDailyAttendanceRequest
{
    [Required] public Guid RegisterId { get; set; }
    public List<UpsertDailyEntryRequest> Entries { get; set; } = [];
}

public class DailyRegisterListFilter
{
    public Guid? SchoolClassId { get; set; }
    public Guid? TermId { get; set; }
    public Guid? SessionId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool? IsSubmitted { get; set; }
}
