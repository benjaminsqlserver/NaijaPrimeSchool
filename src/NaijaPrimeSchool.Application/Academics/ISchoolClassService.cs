using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;

namespace NaijaPrimeSchool.Application.Academics;

public interface ISchoolClassService
{
    Task<IReadOnlyList<SchoolClassDto>> ListAsync(Guid? sessionId = null, CancellationToken ct = default);
    Task<SchoolClassDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<Guid>> CreateAsync(CreateSchoolClassRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateSchoolClassRequest request, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
