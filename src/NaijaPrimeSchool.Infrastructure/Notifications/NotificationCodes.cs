namespace NaijaPrimeSchool.Infrastructure.Notifications;

// Codes of the seeded NotificationChannel / NotificationStatus lookup rows.
internal static class NotificationCodes
{
    public const string Email = "EMAIL";
    public const string Sms = "SMS";

    public const string Pending = "PENDING";
    public const string Sent = "SENT";
    public const string Failed = "FAILED";
    public const string Skipped = "SKIPPED";
}
