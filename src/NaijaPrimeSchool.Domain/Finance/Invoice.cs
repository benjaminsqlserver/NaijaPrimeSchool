using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;

namespace NaijaPrimeSchool.Domain.Finance;

public class Invoice : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid TermId { get; set; }
    public Term? Term { get; set; }

    public Guid SchoolClassId { get; set; }
    public SchoolClass? SchoolClass { get; set; }

    public Guid InvoiceStatusId { get; set; }
    public InvoiceStatus? InvoiceStatus { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssuedOn { get; set; }
    public DateOnly? DueDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }

    public string? Notes { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<PaymentAllocation> Allocations { get; set; } = [];

    public decimal Balance => AmountDue - AmountPaid;
}
