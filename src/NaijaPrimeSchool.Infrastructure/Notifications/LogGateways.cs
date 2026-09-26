using Microsoft.Extensions.Logging;
using NaijaPrimeSchool.Application.Communications;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

// Development gateways: write the message to the log and report success, so
// the whole queue -> dispatch -> Sent pipeline can be exercised without an
// SMTP server or SMS account.
public sealed class LogEmailGateway(ILogger<LogEmailGateway> logger) : IEmailGateway
{
    public Task<GatewayResult> SendAsync(string toAddress, string toName, string subject,
        string textBody, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("[Email:Log] To {Name} <{Address}> | {Subject}\n{Body}",
            toName, toAddress, subject, textBody);
        return Task.FromResult(GatewayResult.Success($"log-{Guid.NewGuid():N}"));
    }
}

public sealed class LogSmsGateway(ILogger<LogSmsGateway> logger) : ISmsGateway
{
    public Task<GatewayResult> SendAsync(string toPhone, string message, CancellationToken ct = default)
    {
        logger.LogInformation("[SMS:Log] To {Phone} ({Length} chars) | {Message}",
            toPhone, message.Length, message);
        return Task.FromResult(GatewayResult.Success($"log-{Guid.NewGuid():N}"));
    }
}
