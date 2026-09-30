using System.Collections.Concurrent;
using NaijaPrimeSchool.Application.Finance;

namespace NaijaPrimeSchool.Infrastructure.Payments;

// Development-only stand-in for Paystack. "Checkout" is a local page
// (/payments/simulator) where the developer chooses the outcome; Verify
// then reports that outcome. Singleton, in-memory: outcomes are lost on
// restart, which just leaves those attempts Pending.
public sealed class SimulatorGateway : IPaymentGateway
{
    public const string CheckoutPath = "/payments/simulator";

    private sealed record Charge(decimal Amount, string Currency, string CallbackUrl)
    {
        public GatewayChargeStatus Status { get; set; } = GatewayChargeStatus.Pending;
        public DateTimeOffset? PaidAt { get; set; }
    }

    private readonly ConcurrentDictionary<string, Charge> charges = new();

    public string Name => "Simulator";

    public Task<GatewayInitializeResult> InitializeAsync(GatewayInitializeRequest request, CancellationToken ct = default)
    {
        charges[request.Reference] = new Charge(request.Amount, request.Currency, request.CallbackUrl);
        return Task.FromResult(new GatewayInitializeResult(true,
            $"{CheckoutPath}?reference={Uri.EscapeDataString(request.Reference)}", null));
    }

    public Task<GatewayVerification> VerifyAsync(string reference, CancellationToken ct = default) =>
        Task.FromResult(charges.TryGetValue(reference, out var c)
            ? new GatewayVerification(c.Status, c.Amount, c.Currency, $"SIM-{reference}", "simulator", c.PaidAt,
                c.Status switch
                {
                    GatewayChargeStatus.Success => "Approved (simulated)",
                    GatewayChargeStatus.Failed => "Declined (simulated)",
                    GatewayChargeStatus.Abandoned => "Cancelled by payer (simulated)",
                    _ => "Awaiting payer (simulated)",
                })
            : new GatewayVerification(GatewayChargeStatus.NotFound, null, null, null, null, null, "Unknown reference (simulator restarted?)"));

    // The simulator never receives webhooks.
    public bool IsValidWebhookSignature(string body, string? signature) => false;
    public string? GetWebhookReference(string body) => null;

    // Called by the simulator checkout page. Returns the callback URL to send
    // the payer back to, or null for an unknown reference.
    public string? Complete(string reference, GatewayChargeStatus outcome)
    {
        if (!charges.TryGetValue(reference, out var c)) return null;
        c.Status = outcome;
        c.PaidAt = outcome == GatewayChargeStatus.Success ? DateTimeOffset.UtcNow : null;
        return c.CallbackUrl;
    }

    public (decimal Amount, string Currency)? Describe(string reference) =>
        charges.TryGetValue(reference, out var c) ? (c.Amount, c.Currency) : null;
}
