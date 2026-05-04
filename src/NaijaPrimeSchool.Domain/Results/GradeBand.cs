using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Results;

public class GradeBand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<SubjectResult> SubjectResults { get; set; } = [];
}
