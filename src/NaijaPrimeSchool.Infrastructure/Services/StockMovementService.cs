using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Inventory;
using NaijaPrimeSchool.Application.Inventory.Dtos;
using NaijaPrimeSchool.Domain.Inventory;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class StockMovementService(ApplicationDbContext db) : IStockMovementService
{
    private static IQueryable<StockMovementDto> Project(IQueryable<StockMovement> q) =>
        q.Select(m => new StockMovementDto
        {
            Id = m.Id,
            StoreItemId = m.StoreItemId,
            StoreItemName = m.StoreItem!.Name,
            StoreItemSku = m.StoreItem!.Sku,
            UnitOfMeasureCode = m.StoreItem!.UnitOfMeasure!.Code,
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
        });

    public async Task<IReadOnlyList<StockMovementDto>> ListAsync(StockMovementFilter filter, CancellationToken ct = default)
    {
        var q = db.StockMovements.AsQueryable();
        if (filter.StoreItemId.HasValue) q = q.Where(m => m.StoreItemId == filter.StoreItemId.Value);
        if (filter.StockMovementTypeId.HasValue) q = q.Where(m => m.StockMovementTypeId == filter.StockMovementTypeId.Value);
        if (filter.SupplierId.HasValue) q = q.Where(m => m.ReceivedFromSupplierId == filter.SupplierId.Value);
        if (filter.Direction.HasValue) q = q.Where(m => m.StockMovementType!.Direction == filter.Direction.Value);
        if (filter.FromDate.HasValue) q = q.Where(m => m.MovedOn >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(m => m.MovedOn <= filter.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(m =>
                m.MovementNumber.ToLower().Contains(term)
                || m.StoreItem!.Name.ToLower().Contains(term)
                || (m.StoreItem!.Sku != null && m.StoreItem!.Sku.ToLower().Contains(term))
                || (m.Reference != null && m.Reference.ToLower().Contains(term)));
        }

        return await Project(q.OrderByDescending(m => m.MovedOn).ThenByDescending(m => m.MovementNumber))
            .ToListAsync(ct);
    }

    public Task<StockMovementDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Project(db.StockMovements.Where(m => m.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> RecordAsync(RecordStockMovementRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0m)
            return OperationResult<Guid>.Failure("Quantity must be greater than zero.");

        var item = await db.StoreItems.FirstOrDefaultAsync(i => i.Id == request.StoreItemId, ct);
        if (item is null) return OperationResult<Guid>.Failure("Store item not found.");

        var type = await db.StockMovementTypes
            .FirstOrDefaultAsync(t => t.Id == request.StockMovementTypeId, ct);
        if (type is null) return OperationResult<Guid>.Failure("Stock movement type not found.");

        if (type.Direction == -1 && request.Quantity > item.QuantityOnHand)
            return OperationResult<Guid>.Failure(
                $"Cannot remove {request.Quantity:N3} {item.QuantityOnHand:N3} are on hand.");

        // Counter-party validation: at most one IssuedTo* and the IsValidForType
        // pairing (purchases name a supplier, issues name a recipient).
        var counterPartyCount = 0;
        if (request.IssuedToStudentId.HasValue) counterPartyCount++;
        if (request.IssuedToSchoolClassId.HasValue) counterPartyCount++;
        if (request.IssuedToUserId.HasValue) counterPartyCount++;
        if (counterPartyCount > 1)
            return OperationResult<Guid>.Failure(
                "A movement can be issued to at most one recipient (pupil, class, or staff member).");

        if (request.ReceivedFromSupplierId.HasValue
            && !await db.Suppliers.AnyAsync(s => s.Id == request.ReceivedFromSupplierId.Value, ct))
            return OperationResult<Guid>.Failure("Selected supplier not found.");

        if (request.IssuedToStudentId.HasValue
            && !await db.Students.AnyAsync(s => s.Id == request.IssuedToStudentId.Value, ct))
            return OperationResult<Guid>.Failure("Selected pupil not found.");

        if (request.IssuedToSchoolClassId.HasValue
            && !await db.SchoolClasses.AnyAsync(c => c.Id == request.IssuedToSchoolClassId.Value, ct))
            return OperationResult<Guid>.Failure("Selected class not found.");

        // Receipt-style sequential numbering, year-prefixed.
        var year = request.MovedOn.Year;
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
        var movementNumber = prefix + (max + 1).ToString("D4");

        var movement = new StockMovement
        {
            StoreItemId = request.StoreItemId,
            StockMovementTypeId = request.StockMovementTypeId,
            MovementNumber = movementNumber,
            MovedOn = request.MovedOn,
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            TotalCost = request.Quantity * request.UnitCost,
            Reference = request.Reference,
            Notes = request.Notes,
            ReceivedFromSupplierId = request.ReceivedFromSupplierId,
            IssuedToStudentId = request.IssuedToStudentId,
            IssuedToSchoolClassId = request.IssuedToSchoolClassId,
            IssuedToUserId = request.IssuedToUserId,
            PerformedById = request.PerformedById,
        };
        db.StockMovements.Add(movement);

        // Update the running balance on the item using Direction.
        item.QuantityOnHand += type.Direction * request.Quantity;
        if (type.Direction == +1 && request.UnitCost > 0m)
        {
            // Latest known unit cost feeds inventory valuation in the dashboard.
            item.LastUnitCost = request.UnitCost;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(movement.Id);
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var movement = await db.StockMovements
            .Include(m => m.StockMovementType)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
        if (movement is null) return OperationResult.Failure("Movement not found.");

        // Reverse the running balance on the item so the audit history can be
        // hidden without leaving a phantom quantity on the catalog.
        var item = await db.StoreItems.FirstOrDefaultAsync(i => i.Id == movement.StoreItemId, ct);
        if (item is not null)
        {
            item.QuantityOnHand -= movement.StockMovementType!.Direction * movement.Quantity;
        }

        db.StockMovements.Remove(movement);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<StoreSummaryDto> GetStoreSummaryAsync(CancellationToken ct = default)
    {
        var items = await db.StoreItems
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Sku,
                i.IsActive,
                i.QuantityOnHand,
                i.ReorderLevel,
                i.LastUnitCost,
                i.ItemCategoryId,
                CategoryName = i.ItemCategory!.Name,
                UnitName = i.UnitOfMeasure!.Name,
                UnitCode = i.UnitOfMeasure!.Code,
            })
            .ToListAsync(ct);

        var monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var movementRows = await db.StockMovements
            .Where(m => m.MovedOn >= monthStart && m.MovedOn < monthEnd)
            .Select(m => new { m.StockMovementType!.Direction })
            .ToListAsync(ct);

        var recent = await Project(db.StockMovements.OrderByDescending(m => m.MovedOn)
                                                    .ThenByDescending(m => m.MovementNumber)
                                                    .Take(20))
            .ToListAsync(ct);

        var summary = new StoreSummaryDto
        {
            TotalItems = items.Count,
            ActiveItems = items.Count(i => i.IsActive),
            ItemsBelowReorder = items.Count(i => i.IsActive && i.QuantityOnHand <= i.ReorderLevel),
            StockValue = items.Sum(i => i.QuantityOnHand * i.LastUnitCost),
            MovementsThisMonth = movementRows.Count,
            InboundThisMonth = movementRows.Count(m => m.Direction == +1),
            OutboundThisMonth = movementRows.Count(m => m.Direction == -1),
            ByCategory = items
                .GroupBy(i => new { i.ItemCategoryId, i.CategoryName })
                .Select(g => new CategoryStockDto
                {
                    ItemCategoryId = g.Key.ItemCategoryId,
                    ItemCategoryName = g.Key.CategoryName,
                    ItemCount = g.Count(),
                    StockValue = g.Sum(i => i.QuantityOnHand * i.LastUnitCost),
                })
                .OrderByDescending(b => b.StockValue)
                .ToList(),
            LowStockItems = items
                .Where(i => i.IsActive && i.QuantityOnHand <= i.ReorderLevel)
                .OrderBy(i => i.QuantityOnHand)
                .Take(15)
                .Select(i => new StoreItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Sku = i.Sku,
                    ItemCategoryId = i.ItemCategoryId,
                    ItemCategoryName = i.CategoryName,
                    UnitOfMeasureName = i.UnitName,
                    UnitOfMeasureCode = i.UnitCode,
                    QuantityOnHand = i.QuantityOnHand,
                    ReorderLevel = i.ReorderLevel,
                    LastUnitCost = i.LastUnitCost,
                    IsActive = i.IsActive,
                })
                .ToList(),
            RecentMovements = recent.ToList(),
        };

        return summary;
    }
}
