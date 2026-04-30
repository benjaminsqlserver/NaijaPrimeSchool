using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Family.Dtos;

namespace NaijaPrimeSchool.Application.Family;

public interface IEnrolmentService
{
    Task<IReadOnlyList<EnrolmentDto>> ListAsync(EnrolmentListFilter filter, CancellationToken ct = default);
    Task<EnrolmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<Guid>> CreateAsync(CreateEnrolmentRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateEnrolmentRequest request, CancellationToken ct = default);
    Task<OperationResult> WithdrawAsync(Guid id, DateOnly withdrawnOn, string? notes, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
