using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;

namespace NaijaPrimeSchool.Domain.Results;

public class AssessmentScore : BaseEntity
{
    public Guid TermAssessmentId { get; set; }
    public TermAssessment? TermAssessment { get; set; }

    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public decimal? Score { get; set; }
    public bool IsAbsent { get; set; }
    public string? Remarks { get; set; }
}
