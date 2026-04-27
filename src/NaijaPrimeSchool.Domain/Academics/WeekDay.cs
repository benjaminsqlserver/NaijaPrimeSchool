using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Academics;

public class WeekDay : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<TimetableEntry> TimetableEntries { get; set; } = [];
}
