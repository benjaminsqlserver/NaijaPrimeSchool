using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Finance;

// One attempt by a parent or student to pay an invoice through the online
// gateway. Only once the gateway confirms the charge (server-to-server
// verification, never the browser redirect alone) is a normal Payment
// recorded and linked here.
public class OnlinePayment : BaseEntity
{
    // Our reference, sent to the gateway and echoed back on callback and
    // webhook. Unique.
    public string Reference { get; set; } = string.Empty;

    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid PayerUserId { get; set; }
    public ApplicationUser? PayerUser { get; set; }
    public string PayerEmail { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NGN";

    // Which gateway handled it ("Paystack", or "Simulator" in development).
    public string Provider { get; set; } = string.Empty;

    public Guid OnlinePaymentStatusId { get; set; }
    public OnlinePaymentStatus? OnlinePaymentStatus { get; set; }

    public string? AuthorizationUrl { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? Channel { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? LastVerifiedOn { get; set; }
    public string? StatusMessage { get; set; }

    // The receipt recorded once the gateway confirmed the charge.
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}
