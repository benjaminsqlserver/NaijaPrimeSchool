using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

internal sealed record ComposedEmail(string Subject, string TextBody, string HtmlBody);

internal static partial class NotificationComposer
{
    // One standard GSM-7 SMS segment.
    public const int SmsMaxLength = 160;

    public static string? PortalLink(NotificationOptions options, string path = "/portal/announcements") =>
        string.IsNullOrWhiteSpace(options.PortalBaseUrl)
            ? null
            : options.PortalBaseUrl.TrimEnd('/') + path;

    public static string MessageThreadPath(Guid threadId) => $"/portal/messages/{threadId}";

    // Alert that the office has written in a portal conversation. The email
    // quotes the unread office messages (oldest first); callers cap how many.
    public static ComposedEmail ComposeMessageEmail(NotificationOptions options, string recipientName,
        Guid threadId, string threadSubject, IReadOnlyList<(string Sender, DateTimeOffset SentOn, string Body)> unread,
        TimeZoneInfo zone)
    {
        var link = PortalLink(options, MessageThreadPath(threadId));
        var subject = Truncate($"{options.SchoolName}: new message — {threadSubject}", 300);
        var intro = unread.Count == 1
            ? "The school office has sent you a message in the portal."
            : $"The school office has sent you {unread.Count} messages in the portal.";

        var text = new StringBuilder()
            .AppendLine($"Dear {recipientName},")
            .AppendLine()
            .AppendLine(intro)
            .AppendLine()
            .AppendLine($"Conversation: {threadSubject}")
            .AppendLine();
        foreach (var m in unread)
        {
            text.AppendLine($"{m.Sender}, {TimeZoneInfo.ConvertTime(m.SentOn, zone):dd MMM yyyy HH:mm}:")
                .AppendLine(m.Body)
                .AppendLine();
        }
        text.AppendLine(link is null ? "Please reply in the portal, under Messages." : $"Reply in the portal: {link}")
            .AppendLine()
            .AppendLine($"— {options.SchoolName}");

        var e = (string s) => WebUtility.HtmlEncode(s);
        var html = new StringBuilder()
            .Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#1f2937;max-width:600px;\">")
            .Append($"<div style=\"background:#0b6b3a;color:#fff;padding:16px 20px;font-size:18px;font-weight:600;\">{e(options.SchoolName)}</div>")
            .Append("<div style=\"padding:20px;border:1px solid #e5e7eb;border-top:none;\">")
            .Append($"<p>Dear {e(recipientName)},</p>")
            .Append($"<p>{e(intro)}</p>")
            .Append($"<h2 style=\"color:#0b6b3a;font-size:18px;margin:20px 0 8px;\">{e(threadSubject)}</h2>");
        foreach (var m in unread)
        {
            html.Append("<div style=\"background:#f4f6f8;border-radius:10px;padding:10px 14px;margin:10px 0;\">")
                .Append($"<div style=\"font-size:12px;color:#6b7280;margin-bottom:4px;\">{e(m.Sender)} · {TimeZoneInfo.ConvertTime(m.SentOn, zone):dd MMM yyyy HH:mm}</div>")
                .Append($"<div style=\"white-space:pre-line;\">{e(m.Body)}</div></div>");
        }
        if (link is not null)
        {
            html.Append($"<p style=\"margin-top:24px;\"><a href=\"{e(link)}\" style=\"background:#d4a017;color:#1f2937;padding:10px 18px;text-decoration:none;border-radius:4px;font-weight:600;\">Reply in the portal</a></p>");
        }
        html.Append($"<p style=\"margin-top:24px;color:#6b7280;font-size:12px;\">Replies to this email are not monitored. Please answer in the portal so the office sees your reply.</p>")
            .Append("</div></div>");

        return new ComposedEmail(subject, text.ToString(), html.ToString());
    }

    // SMS alerts never carry the message text: family phones are often
    // shared, and a conversation may be about a child's health or fees.
    // The portal link (with its thread id) is long, so the fixed wording is
    // kept short and the subject gets whatever room is left, shortened
    // inside its quotes rather than cut off mid-sentence.
    public static string ComposeMessageSms(NotificationOptions options, Guid threadId, string threadSubject)
    {
        var link = PortalLink(options, MessageThreadPath(threadId));
        var prefix = $"{options.SchoolName}: new message from the office re ";
        var suffix = link is null ? ". See Messages in the portal." : ". Reply: " + link;
        var room = SmsMaxLength - prefix.Length - suffix.Length - 2; // 2 for the quotes
        var subject = CollapseWhitespace(threadSubject);
        return room >= 8
            ? $"{prefix}\"{Truncate(subject, room)}\"{suffix}"
            : Truncate($"{options.SchoolName}: new message from the office{suffix}", SmsMaxLength);
    }

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
