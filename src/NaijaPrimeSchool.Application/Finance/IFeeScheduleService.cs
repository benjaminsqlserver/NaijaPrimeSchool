using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance.Dtos;

namespace NaijaPrimeSchool.Application.Finance;

public interface IFeeScheduleService
{
    Task<IReadOnlyList<FeeScheduleDto>> ListAsync(FeeScheduleFilter filter, CancellationToken ct = default);
    Task<FeeScheduleDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<Guid>> CreateAsync(CreateFeeScheduleRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateFeeScheduleRequest request, CancellationToken ct = default);
    Task<OperationResult> PublishAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> UnpublishAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<Guid>> UpsertItemAsync(UpsertFeeScheduleItemRequest request, CancellationToken ct = default);
    Task<OperationResult> RemoveItemAsync(Guid itemId, CancellationToken ct = default);
}
