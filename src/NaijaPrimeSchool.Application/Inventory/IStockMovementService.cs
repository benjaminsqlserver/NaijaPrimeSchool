using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Inventory.Dtos;

namespace NaijaPrimeSchool.Application.Inventory;

public interface IStockMovementService
{
    Task<IReadOnlyList<StockMovementDto>> ListAsync(StockMovementFilter filter, CancellationToken ct = default);
    Task<StockMovementDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<Guid>> RecordAsync(RecordStockMovementRequest request, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<StoreSummaryDto> GetStoreSummaryAsync(CancellationToken ct = default);
}
