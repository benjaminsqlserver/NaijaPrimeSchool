using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Family;

public class Parent : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public Guid? TitleId { get; set; }
    public Title? Title { get; set; }

    public Guid? GenderId { get; set; }
    public Gender? Gender { get; set; }

    public Guid? MaritalStatusId { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }

    public string? PrimaryPhone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? ResidentialAddress { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<StudentParent> StudentLinks { get; set; } = [];

    public string FullName =>
        string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}".Trim()
            : $"{FirstName} {MiddleName} {LastName}".Trim();
}
