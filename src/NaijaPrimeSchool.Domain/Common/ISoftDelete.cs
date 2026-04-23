namespace NaijaPrimeSchool.Domain.Common;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedOn { get; set; }
    string? DeletedBy { get; set; }
}
