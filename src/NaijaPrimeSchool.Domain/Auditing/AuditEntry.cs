namespace NaijaPrimeSchool.Domain.Auditing;

// One recorded change to one row, written by ApplicationDbContext in the
// same SaveChanges (and so the same transaction) as the change itself.
//
// Deliberately NOT a BaseEntity: audit rows are append-only. They are never
// updated, soft-deleted or themselves audited.
public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset OccurredOn { get; set; }

    // Who made the change. UserId is null for background work (e.g. the
    // notification dispatcher or a payment webhook); UserName is then "system".
    // No FK to Users: the trail must outlive (and not constrain) accounts.
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public Guid AuditActionId { get; set; }
    public AuditAction? AuditAction { get; set; }

    // CLR type name of the changed entity, e.g. "Invoice", "ApplicationUser".
    public string EntityType { get; set; } = string.Empty;

    // Primary key of the changed row (composite keys joined with ",").
    public string EntityId { get; set; } = string.Empty;

    // Human-readable name of the row at the time (invoice number, full name,
    // title, ...), so the log stays readable after the row changes or goes.
    public string? EntityLabel { get; set; }

    // JSON array of { "Field", "Old", "New" } for the fields that changed.
    public string Changes { get; set; } = "[]";
}
