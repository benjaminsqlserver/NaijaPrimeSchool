using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Communications;

// Lifecycle of a queued notification: Pending -> Sent | Failed | Skipped.
// The dispatcher only ever picks up Pending rows; Failed and Skipped rows
// go back to Pending only through a manual retry or a re-queue.
public class NotificationStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<AnnouncementNotification> Notifications { get; set; } = [];
}
