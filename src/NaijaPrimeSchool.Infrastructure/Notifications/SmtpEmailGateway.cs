using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Options;
using NaijaPrimeSchool.Application.Communications;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

public sealed class SmtpEmailGateway(IOptions<NotificationOptions> options) : IEmailGateway
{
    public async Task<GatewayResult> SendAsync(string toAddress, string toName, string subject,
        string textBody, string htmlBody, CancellationToken ct = default)
    {
        var email = options.Value.Email;
        var smtp = email.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host))
            return GatewayResult.Failure("Notifications:Email:Smtp:Host is not configured.");

        using var message = new MailMessage
        {
            From = new MailAddress(email.FromAddress, email.FromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false,
        };
        message.To.Add(new MailAddress(toAddress, toName));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html));

        using var client = new SmtpClient(smtp.Host, smtp.Port) { EnableSsl = smtp.EnableSsl };
        if (!string.IsNullOrWhiteSpace(smtp.UserName))
            client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);

        try
        {
            await client.SendMailAsync(message, ct);
            return GatewayResult.Success();
        }
        catch (SmtpException ex)
        {
            return GatewayResult.Failure($"SMTP {ex.StatusCode}: {ex.Message}");
        }
    }
}
