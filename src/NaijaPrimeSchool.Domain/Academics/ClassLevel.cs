using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Academics;

public class ClassLevel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<SchoolClass> Classes { get; set; } = [];
}
