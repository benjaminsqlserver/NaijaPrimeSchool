using NaijaPrimeSchool.Domain.Attendance;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Family;

public class Student : BaseEntity
{
    public string AdmissionNumber { get; set; } = string.Empty;
    public DateOnly AdmissionDate { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public Guid? GenderId { get; set; }
    public Gender? Gender { get; set; }

    public Guid? BloodGroupId { get; set; }
    public BloodGroup? BloodGroup { get; set; }

    public string? StateOfOrigin { get; set; }
    public string? ResidentialAddress { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalNotes { get; set; }

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Enrolment> Enrolments { get; set; } = [];
    public ICollection<StudentParent> ParentLinks { get; set; } = [];
    public ICollection<DailyAttendanceEntry> DailyAttendanceEntries { get; set; } = [];
    public ICollection<SubjectAttendanceEntry> SubjectAttendanceEntries { get; set; } = [];

    public string FullName =>
        string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}".Trim()
            : $"{FirstName} {MiddleName} {LastName}".Trim();
}
