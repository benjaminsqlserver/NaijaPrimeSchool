using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Communications;

public class AnnouncementCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    // Urgent categories (seeded: Emergency) are delivered even during a
    // recipient's quiet hours. Channel opt-outs still apply.
    public bool IsUrgent { get; set; }

    public ICollection<Announcement> Announcements { get; set; } = [];
}
