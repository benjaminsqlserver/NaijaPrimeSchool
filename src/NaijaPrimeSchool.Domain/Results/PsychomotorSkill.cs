using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Results;

public class PsychomotorSkill : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<PsychomotorRating> Ratings { get; set; } = [];
}
