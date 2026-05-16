using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Inventory.Dtos;

namespace NaijaPrimeSchool.Application.Inventory;

public interface IStoreItemService
{
    Task<IReadOnlyList<StoreItemDto>> ListAsync(StoreItemFilter filter, CancellationToken ct = default);
    Task<StoreItemDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<Guid>> CreateAsync(CreateStoreItemRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateStoreItemRequest request, CancellationToken ct = default);
    Task<OperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
