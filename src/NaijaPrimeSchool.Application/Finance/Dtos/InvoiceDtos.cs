using System.ComponentModel.DataAnnotations;

namespace NaijaPrimeSchool.Application.Finance.Dtos;

public class InvoiceDto
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssuedOn { get; set; }
    public DateOnly? DueDate { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;
    public string? StudentPhotoUrl { get; set; }
    public string StudentFirstName { get; set; } = string.Empty;
    public string StudentLastName { get; set; } = string.Empty;

    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public string SessionName { get; set; } = string.Empty;

    public Guid SchoolClassId { get; set; }
    public string SchoolClassName { get; set; } = string.Empty;

    public Guid InvoiceStatusId { get; set; }
    public string InvoiceStatusName { get; set; } = string.Empty;
    public string InvoiceStatusCode { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance => AmountDue - AmountPaid;

    public string? Notes { get; set; }
}

public class InvoiceLineDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid FeeCategoryId { get; set; }
    public string FeeCategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal => Amount - Discount;
}

public class InvoicePaymentDto
{
    public Guid PaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly PaidOn { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal AmountApplied { get; set; }
    public string? Reference { get; set; }
}

public class InvoiceDetailDto
{
    public InvoiceDto Invoice { get; set; } = new();
    public List<InvoiceLineDto> Lines { get; set; } = [];
    public List<InvoicePaymentDto> Payments { get; set; } = [];
}

public class IssueInvoicesRequest
{
    [Required] public Guid FeeScheduleId { get; set; }
    [Required] public Guid SchoolClassId { get; set; }
    [Required] public DateOnly IssuedOn { get; set; }
    public DateOnly? DueDate { get; set; }
    public List<Guid>? StudentIds { get; set; }   // optional — null means every active enrolment
}

public class IssueInvoicesResponse
{
    public int Issued { get; set; }
    public int Skipped { get; set; }
    public List<string> Warnings { get; set; } = [];
}

public class UpdateInvoiceLineDiscountRequest
{
    public Guid InvoiceLineId { get; set; }

    [Range(0, 100000000.0)]
    public decimal Discount { get; set; }
}

public class InvoiceFilter
{
    public Guid? StudentId { get; set; }
    public Guid? TermId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? SchoolClassId { get; set; }
    public Guid? InvoiceStatusId { get; set; }
    public string? Search { get; set; }
}
