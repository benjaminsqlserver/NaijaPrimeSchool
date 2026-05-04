using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Results;

public class TermAssessment : BaseEntity
{
    public Guid TermId { get; set; }
    public Term? Term { get; set; }

    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public Guid AssessmentTypeId { get; set; }
    public AssessmentType? AssessmentType { get; set; }

    public string Title { get; set; } = string.Empty;
    public int MaxScore { get; set; }
    public decimal Weight { get; set; } = 1m;
    public DateOnly? AssessmentDate { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOn { get; set; }

    public string? Notes { get; set; }

    public ICollection<AssessmentScore> Scores { get; set; } = [];
}
