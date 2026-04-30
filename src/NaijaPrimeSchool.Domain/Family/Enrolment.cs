using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Family;

public class Enrolment : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public DateOnly EnrolledOn { get; set; }
    public DateOnly? WithdrawnOn { get; set; }

    public Guid EnrolmentStatusId { get; set; }
    public EnrolmentStatus? EnrolmentStatus { get; set; }

    public string? Notes { get; set; }
}
