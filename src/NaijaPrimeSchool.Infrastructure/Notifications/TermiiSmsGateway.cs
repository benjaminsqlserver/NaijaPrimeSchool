using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NaijaPrimeSchool.Application.Communications;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

// Termii (https://developers.termii.com/messaging-api) — a Nigerian SMS
// gateway. Typed HttpClient; registered through AddHttpClient.
public sealed class TermiiSmsGateway(HttpClient http, IOptions<NotificationOptions> options) : ISmsGateway
{
    public async Task<GatewayResult> SendAsync(string toPhone, string message, CancellationToken ct = default)
    {
        var sms = options.Value.Sms;
        if (string.IsNullOrWhiteSpace(sms.Termii.ApiKey))
            return GatewayResult.Failure("Notifications:Sms:Termii:ApiKey is not configured.");

        var payload = new TermiiSendRequest
        {
            ApiKey = sms.Termii.ApiKey,
            To = toPhone.TrimStart('+'),
            From = sms.SenderId,
            Sms = message,
            Type = "plain",
            Channel = sms.Termii.Channel,
        };

        using var response = await http.PostAsJsonAsync(
            sms.Termii.BaseUrl.TrimEnd('/') + "/api/sms/send", payload, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return GatewayResult.Failure($"Termii HTTP {(int)response.StatusCode}: {Clip(raw)}");

        try
        {
            var body = JsonSerializer.Deserialize<TermiiSendResponse>(raw);
            return string.IsNullOrWhiteSpace(body?.MessageId)
                ? GatewayResult.Failure($"Termii returned no message_id: {Clip(raw)}")
                : GatewayResult.Success(body.MessageId);
        }
        catch (JsonException)
        {
            return GatewayResult.Failure($"Termii returned an unreadable response: {Clip(raw)}");
        }
    }

    private static string Clip(string s) => s.Length <= 300 ? s : s[..300];

    private sealed class TermiiSendRequest
    {
        [JsonPropertyName("api_key")] public string ApiKey { get; init; } = string.Empty;
        [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
        [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
        [JsonPropertyName("sms")] public string Sms { get; init; } = string.Empty;
        [JsonPropertyName("type")] public string Type { get; init; } = "plain";
        [JsonPropertyName("channel")] public string Channel { get; init; } = "generic";
    }

    private sealed class TermiiSendResponse
    {
        [JsonPropertyName("message_id")] public string? MessageId { get; init; }
        [JsonPropertyName("message")] public string? Message { get; init; }
    }
}
