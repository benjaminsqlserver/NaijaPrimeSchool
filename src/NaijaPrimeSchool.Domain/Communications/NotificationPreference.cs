using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Communications;

// How a portal user (parent or student) wants to be reminded about unread
// announcements. At most one row per user; a user without a row gets the
// defaults below (both channels on, no quiet hours).
public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;

    // Quiet hours are wall-clock times in the school's time zone
    // (Notifications:TimeZone). A window may wrap midnight, e.g. 21:00-07:00.
    // Reminders due inside the window are held until it ends, except for
    // announcements whose category IsUrgent.
    public bool QuietHoursEnabled { get; set; }
    public TimeOnly QuietHoursStart { get; set; } = new(21, 0);
    public TimeOnly QuietHoursEnd { get; set; } = new(7, 0);
}
