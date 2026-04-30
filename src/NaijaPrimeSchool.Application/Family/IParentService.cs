using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Family.Dtos;
using NaijaPrimeSchool.Application.Users.Dtos;

namespace NaijaPrimeSchool.Application.Family;

public interface IParentService
{
    Task<PagedResult<ParentDto>> ListAsync(ParentListFilter filter, CancellationToken ct = default);
    Task<ParentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<Guid>> CreateAsync(CreateParentRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateParentRequest request, CancellationToken ct = default);
    Task<OperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<StudentParentDto>> GetStudentLinksAsync(Guid parentId, CancellationToken ct = default);
}
