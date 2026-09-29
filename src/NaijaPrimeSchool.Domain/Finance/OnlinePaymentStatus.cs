using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Finance;

// Lookup for an online payment attempt:
//   PENDING   - sent to the gateway, outcome not known yet
//   SUCCEEDED - verified with the gateway and recorded as a Payment
//   FAILED    - the gateway reports the charge failed
//   ABANDONED - the payer left checkout without paying (may still be retried)
//   REVIEW    - money may have been taken but could not be recorded
//               automatically (e.g. amount mismatch); the bursar reconciles
public class OnlinePaymentStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<OnlinePayment> OnlinePayments { get; set; } = [];
}
