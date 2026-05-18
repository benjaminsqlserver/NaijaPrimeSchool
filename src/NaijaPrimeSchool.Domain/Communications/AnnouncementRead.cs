using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Communications;

public class AnnouncementRead : BaseEntity
{
    public Guid AnnouncementId { get; set; }
    public Announcement? Announcement { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTimeOffset ReadOn { get; set; } = DateTimeOffset.UtcNow;
}
