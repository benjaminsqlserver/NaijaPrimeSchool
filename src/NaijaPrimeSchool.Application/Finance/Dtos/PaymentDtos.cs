using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Finance.Dtos;

public class PaymentDto
{
    public Guid Id { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly PaidOn { get; set; }
    public decimal Amount { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;
    public string? StudentPhotoUrl { get; set; }
    public string StudentFirstName { get; set; } = string.Empty;
    public string StudentLastName { get; set; } = string.Empty;

    public Guid PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;

    public Guid PaymentStatusId { get; set; }
    public string PaymentStatusName { get; set; } = string.Empty;
    public string PaymentStatusCode { get; set; } = string.Empty;

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid? CollectedById { get; set; }
    public string? CollectedByName { get; set; }

    public decimal AmountAllocated { get; set; }
    public decimal AmountUnallocated => Amount - AmountAllocated;
}

public class PaymentAllocationDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal AmountApplied { get; set; }
    public decimal InvoiceBalanceAfter { get; set; }
}

public class PaymentDetailDto
{
    public PaymentDto Payment { get; set; } = new();
    public List<PaymentAllocationDto> Allocations { get; set; } = [];
}

public class AllocationLineRequest
{
    [Required] public Guid InvoiceId { get; set; }

    [Range(0.0, 100000000.0)]
    public decimal AmountApplied { get; set; }
}

public class RecordPaymentRequest
{
    [Required] public Guid StudentId { get; set; }
    [Required] public Guid PaymentMethodId { get; set; }
    [Required] public DateOnly PaidOn { get; set; }

    [Range(0.01, 100000000.0)]
    public decimal Amount { get; set; }

    [StringLength(120)]
    public string? Reference { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }

    public Guid? CollectedById { get; set; }

    public List<AllocationLineRequest> Allocations { get; set; } = [];
}

public class UpdatePaymentRequest
{
    public Guid Id { get; set; }

    [StringLength(120)]
    public string? Reference { get; set; }

    [StringLength(300)]
    public string? Notes { get; set; }
}

public class PaymentFilter
{
    public Guid? StudentId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public Guid? PaymentStatusId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Search { get; set; }
}
