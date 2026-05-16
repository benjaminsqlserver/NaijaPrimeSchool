using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Inventory.Dtos;

public class SupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    public int PurchaseCount { get; set; }
    public decimal TotalPurchased { get; set; }
}

public class CreateSupplierRequest
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ContactName { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierRequest
{
    public Guid Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ContactName { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class SupplierFilter
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
