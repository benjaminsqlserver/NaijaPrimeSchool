using NaijaPrimeSchool.Application.Auditing.Dtos;

namespace NaijaPrimeSchool.Application.Auditing;

// Read-only access to the audit trail. SuperAdmin and HeadTeacher only;
// everyone else gets empty results.
public interface IAuditLogService
{
    Task<AuditLogPage> ListAsync(AuditLogFilter filter, CancellationToken ct = default);

    // Distinct values present in the log, for filter drop-downs.
    Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserNamesAsync(CancellationToken ct = default);
}
