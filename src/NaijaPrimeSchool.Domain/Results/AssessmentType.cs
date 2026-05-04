using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Results;

public class AssessmentType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DefaultMaxScore { get; set; }
    public bool IsExam { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<TermAssessment> TermAssessments { get; set; } = [];
}
