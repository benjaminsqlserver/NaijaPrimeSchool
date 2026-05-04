using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Results;

public class AffectiveTrait : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<AffectiveRating> Ratings { get; set; } = [];
}
