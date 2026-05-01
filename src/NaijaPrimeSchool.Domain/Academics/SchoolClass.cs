using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Academics;

public class SchoolClass : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid ClassLevelId { get; set; }
    public ClassLevel? ClassLevel { get; set; }

    public Guid SessionId { get; set; }
    public Session? Session { get; set; }

    public Guid? ClassTeacherId { get; set; }
    public ApplicationUser? ClassTeacher { get; set; }

    public ICollection<TimetableEntry> TimetableEntries { get; set; } = [];
    public ICollection<Enrolment> Enrolments { get; set; } = [];
    public ICollection<DailyAttendanceRegister> DailyAttendanceRegisters { get; set; } = [];
}
