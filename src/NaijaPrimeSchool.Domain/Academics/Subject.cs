using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Results;

namespace NaijaPrimeSchool.Domain.Academics;

public class Subject : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<TimetableEntry> TimetableEntries { get; set; } = [];
    public ICollection<TermAssessment> TermAssessments { get; set; } = [];
    public ICollection<SubjectResult> SubjectResults { get; set; } = [];
}
