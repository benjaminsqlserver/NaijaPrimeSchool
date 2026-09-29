using NaijaPrimeSchool.Domain.Communications;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

// Shared by queueing and dispatch so both apply exactly the same rules.
internal static class NotificationPreferenceRules
{
    // Non-null when the user has turned this channel off. No preference row
    // means the defaults: every channel on.
    public static string? OptedOutReason(NotificationPreference? preference, string? channelCode) =>
        (preference, channelCode) switch
        {
            ({ EmailEnabled: false }, NotificationCodes.Email) => "Recipient turned off email reminders.",
            ({ SmsEnabled: false }, NotificationCodes.Sms) => "Recipient turned off SMS reminders.",
            _ => null,
        };

    // Non-null when the reminder must wait for the recipient's quiet hours
    // to end; urgent categories go out regardless.
    public static DateTimeOffset? HoldUntil(
        NotificationPreference? preference, AnnouncementCategory? category, DateTimeOffset utcNow, TimeZoneInfo zone)
    {
        if (preference is not { QuietHoursEnabled: true } || category is { IsUrgent: true })
            return null;
        return QuietHours.QuietUntil(utcNow, preference.QuietHoursStart, preference.QuietHoursEnd, zone);
    }
}
