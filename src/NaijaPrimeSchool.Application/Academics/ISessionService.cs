using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;

namespace NaijaPrimeSchool.Application.Academics;

public interface ISessionService
{
    Task<IReadOnlyList<SessionDto>> ListAsync(CancellationToken ct = default);
    Task<SessionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SessionDto?> GetCurrentAsync(CancellationToken ct = default);
    Task<OperationResult<Guid>> CreateAsync(CreateSessionRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateSessionRequest request, CancellationToken ct = default);
    Task<OperationResult> SetCurrentAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
