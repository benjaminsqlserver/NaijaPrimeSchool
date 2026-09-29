using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance.Dtos;

namespace NaijaPrimeSchool.Application.Finance;

// Online fee payment for parents and students. The gateway's word is the
// only proof of payment: ConfirmAsync always re-verifies with the gateway
// server-to-server before recording anything, and is safe to call any
// number of times (callback, webhook, bursar "verify now").
public interface IOnlinePaymentService
{
    OnlinePaymentSettingsDto GetSettings();

    // Parent (for a linked ward) or student (own invoice) only.
    Task<OperationResult<StartOnlinePaymentResult>> StartAsync(StartOnlinePaymentRequest request, CancellationToken ct = default);

    // Verifies with the gateway and, on success, records the Payment.
    Task<OperationResult<OnlinePaymentDto>> ConfirmAsync(string reference, CancellationToken ct = default);

    // Gateway webhook: checks the signature, then confirms the referenced
    // payment. Returns false when the signature is invalid.
    Task<bool> HandleWebhookAsync(string body, string? signature, CancellationToken ct = default);

    // The payer (or finance staff) looking at one attempt, e.g. on return
    // from checkout. Null when not found or not theirs.
    Task<OnlinePaymentDto?> GetByReferenceAsync(string reference, CancellationToken ct = default);

    // Finance staff (SuperAdmin, HeadTeacher, SchoolBursar).
    Task<IReadOnlyList<OnlinePaymentDto>> ListAsync(OnlinePaymentFilter filter, CancellationToken ct = default);
}
