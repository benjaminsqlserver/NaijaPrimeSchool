using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Academics;

public class Subject : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<TimetableEntry> TimetableEntries { get; set; } = [];
}
