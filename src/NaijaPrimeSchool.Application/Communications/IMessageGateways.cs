namespace NaijaPrimeSchool.Application.Communications;

public sealed record GatewayResult(bool Succeeded, string? ProviderMessageId = null, string? Error = null)
{
    public static GatewayResult Success(string? providerMessageId = null) => new(true, providerMessageId);
    public static GatewayResult Failure(string error) => new(false, Error: error);
}

public interface IEmailGateway
{
    Task<GatewayResult> SendAsync(string toAddress, string toName, string subject,
        string textBody, string htmlBody, CancellationToken ct = default);
}

public interface ISmsGateway
{
    Task<GatewayResult> SendAsync(string toPhone, string message, CancellationToken ct = default);
}
