using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Finance;

public class FeeCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsMandatoryByDefault { get; set; }

    public ICollection<FeeScheduleItem> ScheduleItems { get; set; } = [];
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = [];
}
