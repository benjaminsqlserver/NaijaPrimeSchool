using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;

namespace NaijaPrimeSchool.Domain.Attendance;

public class SubjectAttendanceEntry : BaseEntity
{
    public Guid SessionId { get; set; }
    public SubjectAttendanceSession? Session { get; set; }

    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid AttendanceStatusId { get; set; }
    public AttendanceStatus? AttendanceStatus { get; set; }

    public string? Remarks { get; set; }
}
