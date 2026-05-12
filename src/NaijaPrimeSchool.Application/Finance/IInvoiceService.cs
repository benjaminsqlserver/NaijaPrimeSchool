using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance.Dtos;

namespace NaijaPrimeSchool.Application.Finance;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceDto>> ListAsync(InvoiceFilter filter, CancellationToken ct = default);
    Task<InvoiceDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<IssueInvoicesResponse>> IssueAsync(IssueInvoicesRequest request, CancellationToken ct = default);
    Task<OperationResult> SetLineDiscountAsync(UpdateInvoiceLineDiscountRequest request, CancellationToken ct = default);
    Task<OperationResult> CancelAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<StudentLedgerDto?> GetStudentLedgerAsync(Guid studentId, Guid? termId = null, CancellationToken ct = default);
}
