using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Finance;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Domain.Inventory;
using NaijaPrimeSchool.Domain.Results;

namespace NaijaPrimeSchool.Domain.Academics;

public class SchoolClass : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid ClassLevelId { get; set; }
    public ClassLevel? ClassLevel { get; set; }

    public Guid SessionId { get; set; }
    public Session? Session { get; set; }

    public Guid? ClassTeacherId { get; set; }
    public ApplicationUser? ClassTeacher { get; set; }

    public ICollection<TimetableEntry> TimetableEntries { get; set; } = [];
    public ICollection<Enrolment> Enrolments { get; set; } = [];
    public ICollection<DailyAttendanceRegister> DailyAttendanceRegisters { get; set; } = [];
    public ICollection<TermAssessment> TermAssessments { get; set; } = [];
    public ICollection<SubjectResult> SubjectResults { get; set; } = [];
    public ICollection<ReportCard> ReportCards { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<StockMovement> StockIssuances { get; set; } = [];
}
