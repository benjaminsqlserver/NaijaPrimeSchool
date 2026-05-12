using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance.Dtos;

namespace NaijaPrimeSchool.Application.Finance;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> ListAsync(PaymentFilter filter, CancellationToken ct = default);
    Task<PaymentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<Guid>> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdatePaymentRequest request, CancellationToken ct = default);
    Task<OperationResult> RefundAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<FinanceSummaryDto> GetSummaryAsync(Guid? termId, CancellationToken ct = default);
}
