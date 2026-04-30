using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Family;

public class MaritalStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<Parent> Parents { get; set; } = [];
}
