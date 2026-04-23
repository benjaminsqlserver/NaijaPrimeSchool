using Microsoft.AspNetCore.Identity;
using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Identity;

public class ApplicationRole : IdentityRole<Guid>, IAuditable, ISoftDelete
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}
