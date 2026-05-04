using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;

namespace NaijaPrimeSchool.Domain.Results;

public class SubjectResult : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid TermId { get; set; }
    public Term? Term { get; set; }

    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public decimal TotalScore { get; set; }
    public decimal Percentage { get; set; }

    public Guid? GradeBandId { get; set; }
    public GradeBand? GradeBand { get; set; }

    public int? Position { get; set; }
    public int? StudentsInClass { get; set; }

    public string? TeacherComment { get; set; }

    public bool IsFinalised { get; set; }
    public DateTimeOffset? FinalisedOn { get; set; }
}
