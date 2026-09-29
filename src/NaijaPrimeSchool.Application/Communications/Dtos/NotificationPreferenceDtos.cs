namespace NaijaPrimeSchool.Application.Communications.Dtos;

public class NotificationPreferenceDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool QuietHoursEnabled { get; set; }
    public TimeOnly QuietHoursStart { get; set; } = new(21, 0);
    public TimeOnly QuietHoursEnd { get; set; } = new(7, 0);

    // Where reminders would go right now (null = no usable address / number).
    public string? EmailDestination { get; set; }
    public string? SmsDestination { get; set; }

    // School-wide switches from configuration; when false the channel is off
    // for everyone regardless of this user's choice.
    public bool SchoolEmailEnabled { get; set; }
    public bool SchoolSmsEnabled { get; set; }

    // IANA id of the zone quiet hours are interpreted in, e.g. Africa/Lagos.
    public string TimeZone { get; set; } = string.Empty;

    // False until the user (or the office) saves preferences once.
    public bool IsSaved { get; set; }
}

public class UpdateNotificationPreferenceRequest
{
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool QuietHoursEnabled { get; set; }
    public TimeOnly QuietHoursStart { get; set; } = new(21, 0);
    public TimeOnly QuietHoursEnd { get; set; } = new(7, 0);
}
