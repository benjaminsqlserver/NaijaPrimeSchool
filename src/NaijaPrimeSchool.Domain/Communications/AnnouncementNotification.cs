using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Communications;

// One email or SMS nudging a single portal user about an announcement they
// have not read yet. Rows are queued when the announcement is published and
// picked up by the background dispatcher once ScheduledFor has passed; the
// dispatcher re-checks AnnouncementReads at send time and marks the row
// Skipped if the user already read it in the portal.
public class AnnouncementNotification : BaseEntity
{
    public Guid AnnouncementId { get; set; }
    public Announcement? Announcement { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public Guid NotificationChannelId { get; set; }
    public NotificationChannel? NotificationChannel { get; set; }

    public Guid NotificationStatusId { get; set; }
    public NotificationStatus? NotificationStatus { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    // Email address or E.164-ish phone number, resolved at queue time.
    public string Destination { get; set; } = string.Empty;

    public DateTimeOffset ScheduledFor { get; set; }

    // Snapshot of what actually went out, stamped at send time so the log
    // reflects the text the family received even if the notice is edited.
    public string? Subject { get; set; }
    public string? Message { get; set; }

    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptOn { get; set; }
    public DateTimeOffset? SentOn { get; set; }
    public string? LastError { get; set; }
    public string? ProviderMessageId { get; set; }
}
