using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Attendance;

public class DailyAttendanceRegister : BaseEntity
{
    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public Guid TermId { get; set; }
    public Term? Term { get; set; }

    public DateOnly Date { get; set; }

    public Guid? TakenById { get; set; }
    public ApplicationUser? TakenBy { get; set; }
    public DateTimeOffset? TakenOn { get; set; }

    public bool IsSubmitted { get; set; }
    public DateTimeOffset? SubmittedOn { get; set; }

    public string? Notes { get; set; }

    public ICollection<DailyAttendanceEntry> Entries { get; set; } = [];
}
