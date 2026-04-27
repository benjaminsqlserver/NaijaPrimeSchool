using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;

namespace NaijaPrimeSchool.Application.Academics;

public interface ITermService
{
    Task<IReadOnlyList<TermDto>> ListAsync(Guid? sessionId = null, CancellationToken ct = default);
    Task<TermDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TermDto?> GetCurrentAsync(CancellationToken ct = default);
    Task<OperationResult<Guid>> CreateAsync(CreateTermRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateTermRequest request, CancellationToken ct = default);
    Task<OperationResult> SetCurrentAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
