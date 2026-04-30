using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Family.Dtos;
using NaijaPrimeSchool.Application.Users.Dtos;

namespace NaijaPrimeSchool.Application.Family;

public interface IStudentService
{
    Task<PagedResult<StudentDto>> ListAsync(StudentListFilter filter, CancellationToken ct = default);
    Task<StudentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<Guid>> CreateAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateStudentRequest request, CancellationToken ct = default);
    Task<OperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<StudentParentDto>> GetParentLinksAsync(Guid studentId, CancellationToken ct = default);
    Task<OperationResult<Guid>> LinkParentAsync(LinkStudentParentRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateParentLinkAsync(UpdateStudentParentLinkRequest request, CancellationToken ct = default);
    Task<OperationResult> UnlinkParentAsync(Guid linkId, CancellationToken ct = default);
}
