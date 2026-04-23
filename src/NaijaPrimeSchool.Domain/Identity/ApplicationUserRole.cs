using Microsoft.AspNetCore.Identity;

namespace NaijaPrimeSchool.Domain.Identity;

public class ApplicationUserRole : IdentityUserRole<Guid>
{
    public ApplicationUser? User { get; set; }
    public ApplicationRole? Role { get; set; }

    public DateTimeOffset AssignedOn { get; set; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; set; }
}
