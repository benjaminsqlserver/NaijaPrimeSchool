using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Communications;

public class Announcement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public Guid AnnouncementCategoryId { get; set; }
    public AnnouncementCategory? AnnouncementCategory { get; set; }

    public Guid AnnouncementAudienceId { get; set; }
    public AnnouncementAudience? AnnouncementAudience { get; set; }

    // Set only when the audience requires a specific class. Service-layer
    // logic enforces the rule based on AnnouncementAudience.RequiresTargetClass.
    public Guid? TargetSchoolClassId { get; set; }
    public SchoolClass? TargetSchoolClass { get; set; }

    public Guid? PostedById { get; set; }
    public ApplicationUser? PostedBy { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }

    public bool IsPinned { get; set; }

    public ICollection<AnnouncementRead> Reads { get; set; } = [];
}
