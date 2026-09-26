namespace NaijaPrimeSchool.Infrastructure.Notifications;

// Bound from the "Notifications" section of appsettings.json.
public class NotificationOptions
{
    public const string SectionName = "Notifications";

    // Master switch for the background dispatcher. Queueing still works when
    // false (rows simply stay Pending), so turning it back on catches up.
    public bool Enabled { get; set; } = true;

    // Queue unread nudges automatically whenever an announcement is published.
    public bool AutoQueueOnPublish { get; set; } = true;

    // How long a freshly published announcement waits before the nudge goes
    // out. Anyone who reads it in the portal inside this window is skipped.
    public int UnreadGraceMinutes { get; set; } = 120;

    public int DispatchIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 50;
    public int MaxAttempts { get; set; } = 3;

    // Delay before a failed send is retried, multiplied by the attempt number.
    public int RetryDelayMinutes { get; set; } = 5;

    public string SchoolName { get; set; } = "Naija Prime School";

    // Public URL of the web app, used to build the "read it in the portal"
    // link. Leave empty to omit the link.
    public string? PortalBaseUrl { get; set; }

    public EmailChannelOptions Email { get; set; } = new();
    public SmsChannelOptions Sms { get; set; } = new();
}

public class EmailChannelOptions
{
    public bool Enabled { get; set; } = true;

    // "Log" writes messages to the application log instead of sending them;
    // "Smtp" sends through the Smtp settings below.
    public string Provider { get; set; } = "Log";

    public string FromAddress { get; set; } = "no-reply@naijaprimeschool.ng";
    public string FromName { get; set; } = "Naija Prime School";

    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class SmsChannelOptions
{
    public bool Enabled { get; set; } = true;

    // "Log" writes messages to the application log; "Termii" sends through
    // the Termii REST API (https://developers.termii.com).
    public string Provider { get; set; } = "Log";

    // Alphanumeric sender ID registered with the SMS provider (max 11 chars).
    public string SenderId { get; set; } = "NaijaPrime";

    public TermiiOptions Termii { get; set; } = new();
}

public class TermiiOptions
{
    public string BaseUrl { get; set; } = "https://api.ng.termii.com";
    public string? ApiKey { get; set; }

    // "generic" for promotional routes, "dnd" to reach DND-registered numbers.
    public string Channel { get; set; } = "generic";
}
