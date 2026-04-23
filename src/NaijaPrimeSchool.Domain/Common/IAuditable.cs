namespace NaijaPrimeSchool.Domain.Common;

public interface IAuditable
{
    DateTimeOffset CreatedOn { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? ModifiedOn { get; set; }
    string? ModifiedBy { get; set; }
}
