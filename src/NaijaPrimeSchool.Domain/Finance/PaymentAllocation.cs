using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Finance;

public class PaymentAllocation : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public decimal AmountApplied { get; set; }
}
