using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Auditing;

// Lookup: CREATE, UPDATE, DELETE (soft or hard), RESTORE (un-delete).
public class AuditAction : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<AuditEntry> Entries { get; set; } = [];
}
