namespace NaijaPrimeSchool.Application.Auditing.Dtos;

public class AuditEntryDto
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? EntityLabel { get; set; }
    public IReadOnlyList<AuditFieldChangeDto> Changes { get; set; } = [];
}

public class AuditFieldChangeDto
{
    public string Field { get; set; } = string.Empty;
    public string? Old { get; set; }
    public string? New { get; set; }
}

public class AuditLogFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public string? UserName { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? ActionCode { get; set; }
    // Matches the record label, entity id or any changed value.
    public string? Search { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
}

public class AuditLogPage
{
    public IReadOnlyList<AuditEntryDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
