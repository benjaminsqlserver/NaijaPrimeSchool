using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Results.Dtos;

namespace NaijaPrimeSchool.Application.Results;

public interface IReportCardService
{
    Task<IReadOnlyList<ReportCardDto>> ListAsync(ReportCardFilter filter, CancellationToken ct = default);
    Task<ReportCardDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ReportCardDetailDto?> GetForStudentTermAsync(Guid studentId, Guid termId, CancellationToken ct = default);

    Task<OperationResult<GenerateReportCardsResponse>> GenerateAsync(GenerateReportCardsRequest request, CancellationToken ct = default);

    Task<OperationResult> UpdateCommentsAsync(UpdateReportCardCommentsRequest request, CancellationToken ct = default);
    Task<OperationResult> UpsertAffectiveRatingAsync(UpsertAffectiveRatingRequest request, CancellationToken ct = default);
    Task<OperationResult> UpsertPsychomotorRatingAsync(UpsertPsychomotorRatingRequest request, CancellationToken ct = default);

    Task<OperationResult> PublishAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> UnpublishAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
