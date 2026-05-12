namespace NaijaPrimeSchool.Application.Finance.Dtos;

public class StudentLedgerDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentAdmissionNumber { get; set; } = string.Empty;
    public string? StudentPhotoUrl { get; set; }
    public string StudentFirstName { get; set; } = string.Empty;
    public string StudentLastName { get; set; } = string.Empty;

    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance => TotalInvoiced - TotalPaid;

    public List<InvoiceDto> Invoices { get; set; } = [];
    public List<PaymentDto> Payments { get; set; } = [];
}

public class FinanceSummaryDto
{
    public Guid? TermId { get; set; }
    public string? TermName { get; set; }

    public int InvoiceCount { get; set; }
    public int PaidInvoiceCount { get; set; }
    public int PartiallyPaidInvoiceCount { get; set; }
    public int UnpaidInvoiceCount { get; set; }

    public decimal TotalInvoiced { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }

    public int PaymentCount { get; set; }
    public List<MethodBreakdownDto> ByMethod { get; set; } = [];
    public List<CategoryBreakdownDto> ByCategory { get; set; } = [];
}

public class MethodBreakdownDto
{
    public Guid PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int PaymentCount { get; set; }
}

public class CategoryBreakdownDto
{
    public Guid FeeCategoryId { get; set; }
    public string FeeCategoryName { get; set; } = string.Empty;
    public decimal TotalInvoiced { get; set; }
}
