using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Finance;

public class FeeSchedule : BaseEntity
{
    public Guid TermId { get; set; }
    public Term? Term { get; set; }

    public Guid ClassLevelId { get; set; }
    public ClassLevel? ClassLevel { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedOn { get; set; }

    public ICollection<FeeScheduleItem> Items { get; set; } = [];
}
