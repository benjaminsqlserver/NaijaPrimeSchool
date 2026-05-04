using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Results;

public class TraitRating : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<AffectiveRating> AffectiveRatings { get; set; } = [];
    public ICollection<PsychomotorRating> PsychomotorRatings { get; set; } = [];
}
