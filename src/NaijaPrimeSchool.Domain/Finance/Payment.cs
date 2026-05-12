using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Finance;

public class Payment : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    public Guid PaymentStatusId { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly PaidOn { get; set; }
    public decimal Amount { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public Guid? CollectedById { get; set; }
    public ApplicationUser? CollectedBy { get; set; }

    public ICollection<PaymentAllocation> Allocations { get; set; } = [];
}
