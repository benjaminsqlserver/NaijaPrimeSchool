using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Inventory.Dtos;

public class StoreItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }

    public Guid ItemCategoryId { get; set; }
    public string ItemCategoryName { get; set; } = string.Empty;

    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; } = string.Empty;
    public string UnitOfMeasureCode { get; set; } = string.Empty;

    public decimal QuantityOnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal LastUnitCost { get; set; }
    public decimal StockValue => QuantityOnHand * LastUnitCost;

    public bool IsActive { get; set; }
    public bool IsBelowReorder => QuantityOnHand <= ReorderLevel;
}

public class CreateStoreItemRequest
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Sku { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required] public Guid ItemCategoryId { get; set; }
    [Required] public Guid UnitOfMeasureId { get; set; }

    [Range(0.0, 999999999.0)]
    public decimal ReorderLevel { get; set; }

    [Range(0.0, 999999999.0)]
    public decimal OpeningQuantity { get; set; }

    [Range(0.0, 999999999.0)]
    public decimal OpeningUnitCost { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateStoreItemRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Sku { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required] public Guid ItemCategoryId { get; set; }
    [Required] public Guid UnitOfMeasureId { get; set; }

    [Range(0.0, 999999999.0)]
    public decimal ReorderLevel { get; set; }
}

public class StoreItemFilter
{
    public string? Search { get; set; }
    public Guid? ItemCategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? OnlyBelowReorder { get; set; }
}
