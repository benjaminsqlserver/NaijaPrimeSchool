using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Finance;

public class InvoiceStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    // Marks the rows used during the lifecycle:
    //   Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled.
    // The service layer keys off Code rather than Id when transitioning
    // an invoice between states so a row rename does not break logic.

    public ICollection<Invoice> Invoices { get; set; } = [];
}
