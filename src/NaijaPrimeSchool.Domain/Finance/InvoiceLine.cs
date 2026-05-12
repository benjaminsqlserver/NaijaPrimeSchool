using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Finance;

public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public Guid FeeCategoryId { get; set; }
    public FeeCategory? FeeCategory { get; set; }

    public Guid? FeeScheduleItemId { get; set; }
    public FeeScheduleItem? FeeScheduleItem { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }

    public decimal LineTotal => Amount - Discount;
}
