using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NaijaPrimeSchool.Application.Finance;

namespace NaijaPrimeSchool.Infrastructure.Payments;

// Paystack Transactions API (https://paystack.com/docs/api/transaction/).
// Amounts travel in kobo (1 naira = 100 kobo). Typed HttpClient.
public sealed class PaystackGateway(HttpClient http, IOptions<OnlinePaymentOptions> options) : IPaymentGateway
{
    public string Name => "Paystack";

    private PaystackOptions Settings => options.Value.Paystack;

    public async Task<GatewayInitializeResult> InitializeAsync(GatewayInitializeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Settings.SecretKey))
            return new(false, null, "OnlinePayments:Paystack:SecretKey is not configured.");

        using var message = new HttpRequestMessage(HttpMethod.Post, Url("/transaction/initialize"))
        {
            Content = JsonContent.Create(new Dictionary<string, object>
            {
                ["email"] = request.Email,
                ["amount"] = ToKobo(request.Amount),
                ["currency"] = request.Currency,
                ["reference"] = request.Reference,
                ["callback_url"] = request.CallbackUrl,
                ["metadata"] = request.Metadata,
            }),
        };
        Authorize(message);

        using var response = await http.SendAsync(message, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = TryParse(body);
        var data = doc?.RootElement.TryGetProperty("data", out var d) == true ? d : default;

        if (!response.IsSuccessStatusCode || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("authorization_url", out var url) || url.GetString() is not { Length: > 0 } authorizationUrl)
        {
            return new(false, null, $"Paystack HTTP {(int)response.StatusCode}: {MessageOf(doc) ?? Clip(body)}");
        }
        return new(true, authorizationUrl, null);
    }

    public async Task<GatewayVerification> VerifyAsync(string reference, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Settings.SecretKey))
            return new(GatewayChargeStatus.Pending, null, null, null, null, null, "OnlinePayments:Paystack:SecretKey is not configured.");

        using var message = new HttpRequestMessage(HttpMethod.Get, Url($"/transaction/verify/{Uri.EscapeDataString(reference)}"));
        Authorize(message);

        using var response = await http.SendAsync(message, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = TryParse(body);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new(GatewayChargeStatus.NotFound, null, null, null, null, null, MessageOf(doc) ?? "Transaction not found.");
        if (!response.IsSuccessStatusCode || doc is null
            || !doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            // Treat as "don't know yet" so a later verification can settle it.
            return new(GatewayChargeStatus.Pending, null, null, null, null, null,
                $"Paystack HTTP {(int)response.StatusCode}: {MessageOf(doc) ?? Clip(body)}");
        }

        var status = Str(data, "status") switch
        {
            "success" => GatewayChargeStatus.Success,
            "failed" or "reversed" => GatewayChargeStatus.Failed,
            "abandoned" => GatewayChargeStatus.Abandoned,
            _ => GatewayChargeStatus.Pending, // ongoing, pending, processing, queued
        };
        decimal? amount = data.TryGetProperty("amount", out var a) && a.TryGetInt64(out var kobo) ? kobo / 100m : null;
        DateTimeOffset? paidAt = DateTimeOffset.TryParse(Str(data, "paid_at"), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var p) ? p : null;
        var transactionId = data.TryGetProperty("id", out var id) ? id.ToString() : null;

        return new(status, amount, Str(data, "currency"), transactionId, Str(data, "channel"), paidAt,
            Str(data, "gateway_response") ?? MessageOf(doc));
    }

    // Paystack signs the raw request body with HMAC-SHA512 using the secret
    // key and sends the hex digest in the x-paystack-signature header.
    public bool IsValidWebhookSignature(string body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(Settings.SecretKey) || string.IsNullOrWhiteSpace(signature)) return false;
        var expected = HMACSHA512.HashData(Encoding.UTF8.GetBytes(Settings.SecretKey), Encoding.UTF8.GetBytes(body));
        byte[] given;
        try { given = Convert.FromHexString(signature.Trim()); }
        catch (FormatException) { return false; }
        return CryptographicOperations.FixedTimeEquals(expected, given);
    }

    public string? GetWebhookReference(string body)
    {
        using var doc = TryParse(body);
        if (doc is null) return null;
        var root = doc.RootElement;
        // Only charge events carry a payment we care about.
        if (Str(root, "event") is not { } evt || !evt.StartsWith("charge.", StringComparison.Ordinal)) return null;
        return root.TryGetProperty("data", out var data) ? Str(data, "reference") : null;
    }

    public static long ToKobo(decimal naira) => (long)decimal.Round(naira * 100m, 0, MidpointRounding.AwayFromZero);

    private string Url(string path) => Settings.BaseUrl.TrimEnd('/') + path;

    private void Authorize(HttpRequestMessage message) =>
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.SecretKey);

    private static JsonDocument? TryParse(string body)
    {
        try { return JsonDocument.Parse(body); }
        catch (JsonException) { return null; }
    }

    private static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? MessageOf(JsonDocument? doc) => doc is null ? null : Str(doc.RootElement, "message");

    private static string Clip(string s) => s.Length <= 300 ? s : s[..300];
}
