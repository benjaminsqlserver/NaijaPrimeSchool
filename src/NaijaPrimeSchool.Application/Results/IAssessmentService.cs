using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Results.Dtos;

namespace NaijaPrimeSchool.Application.Results;

public interface IAssessmentService
{
    Task<IReadOnlyList<TermAssessmentDto>> ListAsync(TermAssessmentFilter filter, CancellationToken ct = default);
    Task<TermAssessmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<Guid>> CreateAsync(CreateTermAssessmentRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateTermAssessmentRequest request, CancellationToken ct = default);
    Task<OperationResult> PublishAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> UnpublishAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<AssessmentScoreSheetDto?> GetScoreSheetAsync(Guid assessmentId, CancellationToken ct = default);
    Task<OperationResult> UpsertScoreAsync(UpsertAssessmentScoreRequest request, CancellationToken ct = default);
    Task<OperationResult> BulkSetScoresAsync(BulkSetScoresRequest request, CancellationToken ct = default);
}
