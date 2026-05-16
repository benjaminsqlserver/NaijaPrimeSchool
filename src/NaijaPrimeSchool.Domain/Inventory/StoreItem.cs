using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Inventory;

public class StoreItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }

    public Guid ItemCategoryId { get; set; }
    public ItemCategory? ItemCategory { get; set; }

    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }

    public decimal QuantityOnHand { get; set; }
    public decimal ReorderLevel { get; set; }

    public decimal LastUnitCost { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<StockMovement> Movements { get; set; } = [];
}
