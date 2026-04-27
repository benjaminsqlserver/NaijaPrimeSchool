using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Academics;

public class TimetableEntry : BaseEntity
{
    public Guid TermId { get; set; }
    public Term? Term { get; set; }

    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public Guid WeekDayId { get; set; }
    public WeekDay? WeekDay { get; set; }

    public Guid TimetablePeriodId { get; set; }
    public TimetablePeriod? TimetablePeriod { get; set; }

    public Guid? TeacherId { get; set; }
    public ApplicationUser? Teacher { get; set; }

    public string? Room { get; set; }
    public string? Notes { get; set; }
}
