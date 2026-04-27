using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Academics;

public class TermType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<Term> Terms { get; set; } = [];
}
