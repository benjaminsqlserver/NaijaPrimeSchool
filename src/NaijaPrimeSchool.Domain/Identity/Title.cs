using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Identity;

public class Title : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = [];
}
