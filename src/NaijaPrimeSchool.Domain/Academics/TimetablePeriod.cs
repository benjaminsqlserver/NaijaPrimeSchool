using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Academics;

public class TimetablePeriod : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsBreak { get; set; }

    public ICollection<TimetableEntry> TimetableEntries { get; set; } = [];
}
