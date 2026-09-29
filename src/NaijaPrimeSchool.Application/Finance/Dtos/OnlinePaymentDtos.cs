namespace NaijaPrimeSchool.Application.Finance.Dtos;

public class OnlinePaymentDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;

    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    public Guid PayerUserId { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NGN";
    public string Provider { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }

    public string? ProviderTransactionId { get; set; }
    public string? Channel { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? LastVerifiedOn { get; set; }

    public Guid? PaymentId { get; set; }
    public string? ReceiptNumber { get; set; }
}

public class OnlinePaymentFilter
{
    public string? Search { get; set; }
    public string? StatusCode { get; set; }
}

public class StartOnlinePaymentRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }

    // Absolute URL the gateway sends the payer back to; the reference is
    // appended as ?reference=.
    public string ReturnUrl { get; set; } = string.Empty;
}

public class StartOnlinePaymentResult
{
    public string Reference { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
}

public class OnlinePaymentSettingsDto
{
    public bool Enabled { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public decimal MinimumAmount { get; set; }
}
