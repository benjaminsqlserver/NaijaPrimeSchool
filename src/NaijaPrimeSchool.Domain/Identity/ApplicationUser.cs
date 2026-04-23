using Microsoft.AspNetCore.Identity;
using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>, IAuditable, ISoftDelete
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public Guid? TitleId { get; set; }
    public Title? Title { get; set; }

    public Guid? GenderId { get; set; }
    public Gender? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset? DeactivatedOn { get; set; }
    public string? DeactivationReason { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    public string FullName =>
        string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}".Trim()
            : $"{FirstName} {MiddleName} {LastName}".Trim();
}
