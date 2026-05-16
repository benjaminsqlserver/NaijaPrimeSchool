using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Inventory;

public class UnitOfMeasure : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<StoreItem> Items { get; set; } = [];
}
