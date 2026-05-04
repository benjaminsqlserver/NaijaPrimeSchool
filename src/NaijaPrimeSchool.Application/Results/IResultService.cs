using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Results.Dtos;

namespace NaijaPrimeSchool.Application.Results;

public interface IResultService
{
    Task<IReadOnlyList<SubjectResultDto>> ListAsync(SubjectResultFilter filter, CancellationToken ct = default);
    Task<SubjectResultDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<ComputeResultsResponse>> ComputeAsync(ComputeResultsRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateCommentAsync(UpdateSubjectResultRequest request, CancellationToken ct = default);
    Task<OperationResult> FinaliseAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> ReopenAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
