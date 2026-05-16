using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Inventory.Dtos;

namespace NaijaPrimeSchool.Application.Inventory;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> ListAsync(SupplierFilter filter, CancellationToken ct = default);
    Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<Guid>> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateSupplierRequest request, CancellationToken ct = default);
    Task<OperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
