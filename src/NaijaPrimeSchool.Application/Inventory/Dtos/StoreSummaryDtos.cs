namespace NaijaPrimeSchool.Application.Inventory.Dtos;

public class StoreSummaryDto
{
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int ItemsBelowReorder { get; set; }
    public decimal StockValue { get; set; }
    public int MovementsThisMonth { get; set; }
    public int InboundThisMonth { get; set; }
    public int OutboundThisMonth { get; set; }

    public List<CategoryStockDto> ByCategory { get; set; } = [];
    public List<StoreItemDto> LowStockItems { get; set; } = [];
    public List<StockMovementDto> RecentMovements { get; set; } = [];
}

public class CategoryStockDto
{
    public Guid ItemCategoryId { get; set; }
    public string ItemCategoryName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal StockValue { get; set; }
}

public class StoreItemDetailDto
{
    public StoreItemDto Item { get; set; } = new();
    public List<StockMovementDto> Movements { get; set; } = [];
}
