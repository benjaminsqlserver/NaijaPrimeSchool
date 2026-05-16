using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Inventory.Dtos;

public class StockMovementDto
{
    public Guid Id { get; set; }

    public Guid StoreItemId { get; set; }
    public string StoreItemName { get; set; } = string.Empty;
    public string? StoreItemSku { get; set; }
    public string UnitOfMeasureCode { get; set; } = string.Empty;

    public Guid StockMovementTypeId { get; set; }
    public string StockMovementTypeName { get; set; } = string.Empty;
    public string StockMovementTypeCode { get; set; } = string.Empty;
    public int Direction { get; set; }

    public string MovementNumber { get; set; } = string.Empty;
    public DateOnly MovedOn { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid? ReceivedFromSupplierId { get; set; }
    public string? ReceivedFromSupplierName { get; set; }

    public Guid? IssuedToStudentId { get; set; }
    public string? IssuedToStudentName { get; set; }
    public string? IssuedToStudentAdmissionNumber { get; set; }
    public string? IssuedToStudentPhotoUrl { get; set; }
    public string? IssuedToStudentFirstName { get; set; }
    public string? IssuedToStudentLastName { get; set; }

    public Guid? IssuedToSchoolClassId { get; set; }
    public string? IssuedToSchoolClassName { get; set; }

    public Guid? IssuedToUserId { get; set; }
    public string? IssuedToUserName { get; set; }

    public Guid? PerformedById { get; set; }
    public string? PerformedByName { get; set; }
}

public class RecordStockMovementRequest
{
    [Required] public Guid StoreItemId { get; set; }
    [Required] public Guid StockMovementTypeId { get; set; }
    [Required] public DateOnly MovedOn { get; set; }

    [Range(0.01, 999999999.0)]
    public decimal Quantity { get; set; }

    [Range(0.0, 999999999.0)]
    public decimal UnitCost { get; set; }

    [StringLength(120)]
    public string? Reference { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    public Guid? ReceivedFromSupplierId { get; set; }
    public Guid? IssuedToStudentId { get; set; }
    public Guid? IssuedToSchoolClassId { get; set; }
    public Guid? IssuedToUserId { get; set; }
    public Guid? PerformedById { get; set; }
}

public class StockMovementFilter
{
    public Guid? StoreItemId { get; set; }
    public Guid? StockMovementTypeId { get; set; }
    public Guid? SupplierId { get; set; }
    public int? Direction { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Search { get; set; }
}
