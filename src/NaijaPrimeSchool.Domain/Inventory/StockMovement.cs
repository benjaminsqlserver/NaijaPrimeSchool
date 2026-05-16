using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Inventory;

public class StockMovement : BaseEntity
{
    public Guid StoreItemId { get; set; }
    public StoreItem? StoreItem { get; set; }

    public Guid StockMovementTypeId { get; set; }
    public StockMovementType? StockMovementType { get; set; }

    public string MovementNumber { get; set; } = string.Empty;
    public DateOnly MovedOn { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    // Optional counter-parties. At most one of the IssuedTo* should be set on
    // any one row; service-layer logic enforces the rule. ReceivedFromSupplier
    // is independent and used by purchase / return-to-vendor movements.
    public Guid? ReceivedFromSupplierId { get; set; }
    public Supplier? ReceivedFromSupplier { get; set; }

    public Guid? IssuedToStudentId { get; set; }
    public Student? IssuedToStudent { get; set; }

    public Guid? IssuedToSchoolClassId { get; set; }
    public SchoolClass? IssuedToSchoolClass { get; set; }

    public Guid? IssuedToUserId { get; set; }
    public ApplicationUser? IssuedToUser { get; set; }

    public Guid? PerformedById { get; set; }
    public ApplicationUser? PerformedBy { get; set; }
}
