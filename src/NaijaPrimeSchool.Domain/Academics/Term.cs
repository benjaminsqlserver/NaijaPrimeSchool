using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Academics;

public class Term : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session? Session { get; set; }

    public Guid TermTypeId { get; set; }
    public TermType? TermType { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }

    public ICollection<TimetableEntry> TimetableEntries { get; set; } = [];
    public ICollection<DailyAttendanceRegister> DailyAttendanceRegisters { get; set; } = [];
}
