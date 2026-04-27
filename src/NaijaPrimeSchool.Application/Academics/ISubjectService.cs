using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;

namespace NaijaPrimeSchool.Application.Academics;

public interface ISubjectService
{
    Task<IReadOnlyList<SubjectDto>> ListAsync(CancellationToken ct = default);
    Task<SubjectDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<Guid>> CreateAsync(CreateSubjectRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateSubjectRequest request, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
