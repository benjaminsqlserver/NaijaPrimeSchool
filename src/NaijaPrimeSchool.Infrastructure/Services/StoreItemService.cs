using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Inventory;
using NaijaPrimeSchool.Application.Inventory.Dtos;
using NaijaPrimeSchool.Domain.Inventory;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class StoreItemService(ApplicationDbContext db) : IStoreItemService
{
    private static IQueryable<StoreItemDto> Project(IQueryable<StoreItem> q) =>
        q.Select(i => new StoreItemDto
        {
            Id = i.Id,
            Name = i.Name,
            Sku = i.Sku,
            Description = i.Description,
            ItemCategoryId = i.ItemCategoryId,
            ItemCategoryName = i.ItemCategory!.Name,
            UnitOfMeasureId = i.UnitOfMeasureId,
            UnitOfMeasureName = i.UnitOfMeasure!.Name,
            UnitOfMeasureCode = i.UnitOfMeasure!.Code,
            QuantityOnHand = i.QuantityOnHand,
            ReorderLevel = i.ReorderLevel,
            LastUnitCost = i.LastUnitCost,
            IsActive = i.IsActive,
        });

    public async Task<IReadOnlyList<StoreItemDto>> ListAsync(StoreItemFilter filter, CancellationToken ct = default)
    {
        var q = db.StoreItems.AsQueryable();
        if (filter.ItemCategoryId.HasValue) q = q.Where(i => i.ItemCategoryId == filter.ItemCategoryId.Value);
        if (filter.IsActive.HasValue) q = q.Where(i => i.IsActive == filter.IsActive.Value);
        if (filter.OnlyBelowReorder == true) q = q.Where(i => i.QuantityOnHand <= i.ReorderLevel);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(i =>
                i.Name.ToLower().Contains(term)
                || (i.Sku != null && i.Sku.ToLower().Contains(term)));
        }
        return await Project(q.OrderBy(i => i.Name)).ToListAsync(ct);
    }

    public async Task<StoreItemDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await Project(db.StoreItems.Where(i => i.Id == id)).FirstOrDefaultAsync(ct);
        if (item is null) return null;

        var movements = await db.StockMovements
            .Where(m => m.StoreItemId == id)
            .OrderByDescending(m => m.MovedOn)
            .ThenByDescending(m => m.MovementNumber)
            .Take(100)
            .Select(m => new StockMovementDto
            {
                Id = m.Id,
                StoreItemId = m.StoreItemId,
                StoreItemName = item.Name,
                StoreItemSku = item.Sku,
                UnitOfMeasureCode = item.UnitOfMeasureCode,
                StockMovementTypeId = m.StockMovementTypeId,
                StockMovementTypeName = m.StockMovementType!.Name,
                StockMovementTypeCode = m.StockMovementType!.Code,
                Direction = m.StockMovementType!.Direction,
                MovementNumber = m.MovementNumber,
                MovedOn = m.MovedOn,
                Quantity = m.Quantity,
                UnitCost = m.UnitCost,
                TotalCost = m.TotalCost,
                Reference = m.Reference,
                Notes = m.Notes,
                ReceivedFromSupplierId = m.ReceivedFromSupplierId,
                ReceivedFromSupplierName = m.ReceivedFromSupplier == null
                    ? null : m.ReceivedFromSupplier.Name,
                IssuedToStudentId = m.IssuedToStudentId,
                IssuedToStudentName = m.IssuedToStudent == null
                    ? null : (m.IssuedToStudent!.FirstName + " " + m.IssuedToStudent!.LastName).Trim(),
                IssuedToStudentAdmissionNumber = m.IssuedToStudent == null
                    ? null : m.IssuedToStudent!.AdmissionNumber,
                IssuedToStudentPhotoUrl = m.IssuedToStudent == null
                    ? null : m.IssuedToStudent!.PhotoUrl,
                IssuedToStudentFirstName = m.IssuedToStudent == null
                    ? null : m.IssuedToStudent!.FirstName,
                IssuedToStudentLastName = m.IssuedToStudent == null
                    ? null : m.IssuedToStudent!.LastName,
                IssuedToSchoolClassId = m.IssuedToSchoolClassId,
                IssuedToSchoolClassName = m.IssuedToSchoolClass == null
                    ? null : m.IssuedToSchoolClass!.Name,
                IssuedToUserId = m.IssuedToUserId,
                IssuedToUserName = m.IssuedToUser == null
                    ? null : (m.IssuedToUser!.FirstName + " " + m.IssuedToUser!.LastName).Trim(),
                PerformedById = m.PerformedById,
                PerformedByName = m.PerformedBy == null
                    ? null : (m.PerformedBy!.FirstName + " " + m.PerformedBy!.LastName).Trim(),
            })
            .ToListAsync(ct);

        return new StoreItemDetailDto { Item = item, Movements = movements };
    }

    public async Task<OperationResult<Guid>> CreateAsync(CreateStoreItemRequest request, CancellationToken ct = default)
    {
        if (!await db.ItemCategories.AnyAsync(c => c.Id == request.ItemCategoryId, ct))
            return OperationResult<Guid>.Failure("Item category not found.");

        if (!await db.UnitsOfMeasure.AnyAsync(u => u.Id == request.UnitOfMeasureId, ct))
            return OperationResult<Guid>.Failure("Unit of measure not found.");

        if (!string.IsNullOrWhiteSpace(request.Sku)
            && await db.StoreItems.AnyAsync(i => i.Sku == request.Sku, ct))
            return OperationResult<Guid>.Failure(
                $"An item with SKU '{request.Sku}' already exists.");

        var item = new StoreItem
        {
            Name = request.Name.Trim(),
            Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim(),
            Description = request.Description,
            ItemCategoryId = request.ItemCategoryId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            ReorderLevel = request.ReorderLevel,
            QuantityOnHand = 0m,
            LastUnitCost = request.OpeningUnitCost,
            IsActive = request.IsActive,
        };
        db.StoreItems.Add(item);

        if (request.OpeningQuantity > 0m)
        {
            var openingTypeId = await db.StockMovementTypes
                .Where(t => t.Code == "OPENING")
                .Select(t => t.Id)
                .FirstOrDefaultAsync(ct);
            if (openingTypeId == Guid.Empty)
                return OperationResult<Guid>.Failure(
                    "Stock movement types are not seeded.");

            var movementNumber = await NextMovementNumberAsync(request.OpeningUnitCost > 0 ? DateTime.UtcNow.Year : DateTime.UtcNow.Year, ct);

            db.StockMovements.Add(new StockMovement
            {
                StoreItem = item,
                StockMovementTypeId = openingTypeId,
                MovementNumber = movementNumber,
                MovedOn = DateOnly.FromDateTime(DateTime.UtcNow),
                Quantity = request.OpeningQuantity,
                UnitCost = request.OpeningUnitCost,
                TotalCost = request.OpeningQuantity * request.OpeningUnitCost,
                Reference = "Opening balance",
            });

            item.QuantityOnHand = request.OpeningQuantity;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(item.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateStoreItemRequest request, CancellationToken ct = default)
    {
        var item = await db.StoreItems.FirstOrDefaultAsync(i => i.Id == request.Id, ct);
        if (item is null) return OperationResult.Failure("Item not found.");

        if (!await db.ItemCategories.AnyAsync(c => c.Id == request.ItemCategoryId, ct))
            return OperationResult.Failure("Item category not found.");

        if (!await db.UnitsOfMeasure.AnyAsync(u => u.Id == request.UnitOfMeasureId, ct))
            return OperationResult.Failure("Unit of measure not found.");

        if (!string.IsNullOrWhiteSpace(request.Sku)
            && await db.StoreItems.AnyAsync(i => i.Sku == request.Sku && i.Id != request.Id, ct))
            return OperationResult.Failure(
                $"An item with SKU '{request.Sku}' already exists.");

        item.Name = request.Name.Trim();
        item.Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        item.Description = request.Description;
        item.ItemCategoryId = request.ItemCategoryId;
        item.UnitOfMeasureId = request.UnitOfMeasureId;
        item.ReorderLevel = request.ReorderLevel;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var item = await db.StoreItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return OperationResult.Failure("Item not found.");

        item.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.StoreItems
            .Include(i => i.Movements)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return OperationResult.Failure("Item not found.");

        if (item.Movements.Any())
            return OperationResult.Failure(
                "Cannot delete an item with movement history. Deactivate instead.");

        db.StoreItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    private async Task<string> NextMovementNumberAsync(int year, CancellationToken ct)
    {
        var prefix = $"NPS/STK/{year}/";
        var existing = await db.StockMovements
            .Where(m => m.MovementNumber.StartsWith(prefix))
            .Select(m => m.MovementNumber)
            .ToListAsync(ct);
        var max = 0;
        foreach (var n in existing)
        {
            if (int.TryParse(n[prefix.Length..], out var seq) && seq > max) max = seq;
        }
        return prefix + (max + 1).ToString("D4");
    }
}
