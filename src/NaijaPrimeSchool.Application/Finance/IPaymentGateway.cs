namespace NaijaPrimeSchool.Application.Finance;

public sealed record GatewayInitializeRequest(
    string Reference, decimal Amount, string Currency, string Email, string CallbackUrl,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record GatewayInitializeResult(bool Succeeded, string? AuthorizationUrl, string? Error);

public enum GatewayChargeStatus { Pending, Success, Failed, Abandoned, NotFound }

public sealed record GatewayVerification(
    GatewayChargeStatus Status,
    decimal? Amount,
    string? Currency,
    string? TransactionId,
    string? Channel,
    DateTimeOffset? PaidAt,
    string? Message);

// A card / bank-transfer payment provider (Paystack in production, a local
// simulator in development).
public interface IPaymentGateway
{
    string Name { get; }

    Task<GatewayInitializeResult> InitializeAsync(GatewayInitializeRequest request, CancellationToken ct = default);
    Task<GatewayVerification> VerifyAsync(string reference, CancellationToken ct = default);

    // Webhook authenticity check and reference extraction.
    bool IsValidWebhookSignature(string body, string? signature);
    string? GetWebhookReference(string body);
}
