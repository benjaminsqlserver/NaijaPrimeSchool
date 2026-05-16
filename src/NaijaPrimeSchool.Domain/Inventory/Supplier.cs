using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Inventory;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<StockMovement> Purchases { get; set; } = [];
}
