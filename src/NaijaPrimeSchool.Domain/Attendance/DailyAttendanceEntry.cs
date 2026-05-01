using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;

namespace NaijaPrimeSchool.Domain.Attendance;

public class DailyAttendanceEntry : BaseEntity
{
    public Guid RegisterId { get; set; }
    public DailyAttendanceRegister? Register { get; set; }

    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid AttendanceStatusId { get; set; }
    public AttendanceStatus? AttendanceStatus { get; set; }

    public TimeOnly? ArrivalTime { get; set; }
    public string? Remarks { get; set; }
}
