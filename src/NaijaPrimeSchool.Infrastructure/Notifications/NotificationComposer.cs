using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

internal sealed record ComposedEmail(string Subject, string TextBody, string HtmlBody);

internal static partial class NotificationComposer
{
    // One standard GSM-7 SMS segment.
    public const int SmsMaxLength = 160;

    public static string? PortalLink(NotificationOptions options) =>
        string.IsNullOrWhiteSpace(options.PortalBaseUrl)
            ? null
            : options.PortalBaseUrl.TrimEnd('/') + "/portal/announcements";

    public static ComposedEmail ComposeEmail(NotificationOptions options, string recipientName, string title, string body)
    {
        var link = PortalLink(options);
        var subject = Truncate($"{options.SchoolName}: {title}", 300);

        var text = new StringBuilder()
            .AppendLine($"Dear {recipientName},")
            .AppendLine()
            .AppendLine($"A notice was posted on the {options.SchoolName} portal and it is still unread on your account.")
            .AppendLine()
            .AppendLine(title)
            .AppendLine(new string('-', Math.Min(title.Length, 60)))
            .AppendLine(body)
            .AppendLine();
        if (link is not null)
            text.AppendLine($"Read it in the portal: {link}").AppendLine();
        text.AppendLine($"— {options.SchoolName}");

        var e = (string s) => WebUtility.HtmlEncode(s);
        var html = new StringBuilder()
            .Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#1f2937;max-width:600px;\">")
            .Append($"<div style=\"background:#0b6b3a;color:#fff;padding:16px 20px;font-size:18px;font-weight:600;\">{e(options.SchoolName)}</div>")
            .Append("<div style=\"padding:20px;border:1px solid #e5e7eb;border-top:none;\">")
            .Append($"<p>Dear {e(recipientName)},</p>")
            .Append($"<p>A notice was posted on the {e(options.SchoolName)} portal and it is still unread on your account.</p>")
            .Append($"<h2 style=\"color:#0b6b3a;font-size:20px;margin:24px 0 8px;\">{e(title)}</h2>")
            .Append($"<p style=\"white-space:pre-line;\">{e(body)}</p>");
        if (link is not null)
        {
            html.Append($"<p style=\"margin-top:24px;\"><a href=\"{e(link)}\" style=\"background:#d4a017;color:#1f2937;padding:10px 18px;text-decoration:none;border-radius:4px;font-weight:600;\">Open the portal</a></p>");
        }
        html.Append($"<p style=\"margin-top:24px;color:#6b7280;font-size:12px;\">You are receiving this because you have a portal account at {e(options.SchoolName)}.</p>")
            .Append("</div></div>");

        return new ComposedEmail(subject, text.ToString(), html.ToString());
    }

    // "<School>: <Title> - <body…> <link>", squeezed into one SMS segment.
    // Uses "..." rather than an ellipsis character so the message stays in
    // the GSM-7 alphabet (a single non-GSM character halves the budget).
    public static string ComposeSms(NotificationOptions options, string title, string body)
    {
        var link = PortalLink(options);
        var suffix = link is null ? string.Empty : " " + link;
        var budget = SmsMaxLength - suffix.Length;

        var head = CollapseWhitespace($"{options.SchoolName}: {title} - {body}");
        return Truncate(head, budget) + suffix;
    }

    private static string CollapseWhitespace(string s) => Whitespace().Replace(s, " ").Trim();

    private static string Truncate(string s, int max)
    {
        if (max <= 3) return s[..Math.Max(0, max)];
        return s.Length <= max ? s : s[..(max - 3)].TrimEnd() + "...";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
