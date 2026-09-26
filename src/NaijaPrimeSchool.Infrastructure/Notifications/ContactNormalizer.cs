using System.Net.Mail;
using System.Text;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

internal static class ContactNormalizer
{
    // Returns the trimmed address when it parses as a single mailbox.
    public static string? NormalizeEmail(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.Trim();
        return MailAddress.TryCreate(trimmed, out var parsed) && parsed.Address == trimmed ? trimmed : null;
    }

    // Normalises Nigerian numbers to +234XXXXXXXXXX. Local 11-digit numbers
    // ("0803 123 4567") drop the leading zero; numbers already carrying a
    // country code are kept as long as they look like E.164 (8-15 digits).
    public static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var digits = new StringBuilder();
        foreach (var ch in raw.Trim())
        {
            if (char.IsDigit(ch)) digits.Append(ch);
            else if (ch is ' ' or '-' or '(' or ')' or '.' or '+') continue;
            else return null;
        }

        var d = digits.ToString();
        if (d.StartsWith("00")) d = d[2..];

        if (d.Length == 11 && d.StartsWith('0')) return "+234" + d[1..];
        if (d.Length == 10 && !d.StartsWith('0')) return "+234" + d;
        if (d.Length is >= 8 and <= 15 && !d.StartsWith('0')) return "+" + d;
        return null;
    }
}
