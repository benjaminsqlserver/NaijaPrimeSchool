using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Communications;

public class AnnouncementAudience : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    // True when the audience targets a specific class (the Announcement
    // row must then carry a TargetSchoolClassId); false for broad audiences
    // like All, Parents, Students.
    public bool RequiresTargetClass { get; set; }

    public ICollection<Announcement> Announcements { get; set; } = [];
}
