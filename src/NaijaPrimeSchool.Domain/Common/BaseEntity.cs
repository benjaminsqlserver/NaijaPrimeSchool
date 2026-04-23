namespace NaijaPrimeSchool.Domain.Common;

public abstract class BaseEntity : IAuditable, ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}
