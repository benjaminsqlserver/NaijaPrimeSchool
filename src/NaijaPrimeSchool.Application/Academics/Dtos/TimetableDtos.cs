using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Academics.Dtos;

public class TimetablePeriodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsBreak { get; set; }
}

public class CreateTimetablePeriodRequest
{
    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required] public TimeOnly StartTime { get; set; }
    [Required] public TimeOnly EndTime { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsBreak { get; set; }
}

public class UpdateTimetablePeriodRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required] public TimeOnly StartTime { get; set; }
    [Required] public TimeOnly EndTime { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsBreak { get; set; }
}

public class TimetableEntryDto
{
    public Guid Id { get; set; }
    public Guid TermId { get; set; }
    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public Guid WeekDayId { get; set; }
    public string WeekDayName { get; set; } = string.Empty;
    public int WeekDayOrder { get; set; }
    public Guid TimetablePeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public TimeOnly PeriodStart { get; set; }
    public TimeOnly PeriodEnd { get; set; }
    public int PeriodOrder { get; set; }
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? Room { get; set; }
    public string? Notes { get; set; }
}

public class UpsertTimetableEntryRequest
{
    public Guid? Id { get; set; }
    [Required] public Guid TermId { get; set; }
    [Required] public Guid SchoolClassId { get; set; }
    [Required] public Guid SubjectId { get; set; }
    [Required] public Guid WeekDayId { get; set; }
    [Required] public Guid TimetablePeriodId { get; set; }
    public Guid? TeacherId { get; set; }

    [StringLength(60)]
    public string? Room { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}

public class TimetableQuery
{
    public Guid TermId { get; set; }
    public Guid SchoolClassId { get; set; }
}
