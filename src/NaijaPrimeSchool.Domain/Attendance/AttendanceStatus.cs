using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Attendance;

public class AttendanceStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool CountsAsPresent { get; set; }

    public ICollection<DailyAttendanceEntry> DailyEntries { get; set; } = [];
    public ICollection<SubjectAttendanceEntry> SubjectEntries { get; set; } = [];
}
