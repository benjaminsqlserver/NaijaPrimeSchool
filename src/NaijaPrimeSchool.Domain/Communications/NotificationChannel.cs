using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Communications;

// Delivery channel for an out-of-portal notification (Email, SMS).
public class NotificationChannel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<AnnouncementNotification> Notifications { get; set; } = [];
}
