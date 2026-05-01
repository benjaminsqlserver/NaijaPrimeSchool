using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Attendance;

public class SubjectAttendanceSession : BaseEntity
{
    public Guid TimetableEntryId { get; set; }
    public TimetableEntry? TimetableEntry { get; set; }

    public DateOnly Date { get; set; }

    public Guid? TakenById { get; set; }
    public ApplicationUser? TakenBy { get; set; }
    public DateTimeOffset? TakenOn { get; set; }

    public bool IsSubmitted { get; set; }
    public DateTimeOffset? SubmittedOn { get; set; }

    public string? Notes { get; set; }

    public ICollection<SubjectAttendanceEntry> Entries { get; set; } = [];
}
