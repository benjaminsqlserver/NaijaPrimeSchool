using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Inventory;
using NaijaPrimeSchool.Application.Inventory.Dtos;
using NaijaPrimeSchool.Domain.Inventory;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class SupplierService(ApplicationDbContext db) : ISupplierService
{
    private static IQueryable<SupplierDto> Project(IQueryable<Supplier> q) =>
        q.Select(s => new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            ContactName = s.ContactName,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            Notes = s.Notes,
            IsActive = s.IsActive,
            PurchaseCount = s.Purchases.Count,
            TotalPurchased = s.Purchases.Sum(p => (decimal?)p.TotalCost) ?? 0m,
        });

    public async Task<IReadOnlyList<SupplierDto>> ListAsync(SupplierFilter filter, CancellationToken ct = default)
    {
        var q = db.Suppliers.AsQueryable();
        if (filter.IsActive.HasValue) q = q.Where(s => s.IsActive == filter.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(s =>
                s.Name.ToLower().Contains(term)
                || (s.ContactName != null && s.ContactName.ToLower().Contains(term))
                || (s.Phone != null && s.Phone.Contains(term))
                || (s.Email != null && s.Email.ToLower().Contains(term)));
        }
        return await Project(q.OrderBy(s => s.Name)).ToListAsync(ct);
    }

    public Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Project(db.Suppliers.Where(s => s.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        if (await db.Suppliers.AnyAsync(s => s.Name == request.Name, ct))
            return OperationResult<Guid>.Failure(
                $"A supplier named '{request.Name}' already exists.");

        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactName = request.ContactName,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            Notes = request.Notes,
            IsActive = request.IsActive,
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(supplier.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (supplier is null) return OperationResult.Failure("Supplier not found.");

        if (await db.Suppliers.AnyAsync(s => s.Name == request.Name && s.Id != request.Id, ct))
            return OperationResult.Failure(
                $"A different supplier named '{request.Name}' already exists.");

        supplier.Name = request.Name.Trim();
        supplier.ContactName = request.ContactName;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        supplier.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null) return OperationResult.Failure("Supplier not found.");

        supplier.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers
            .Include(s => s.Purchases)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null) return OperationResult.Failure("Supplier not found.");

        if (supplier.Purchases.Any())
            return OperationResult.Failure(
                "Cannot delete a supplier with purchase history. Deactivate instead.");

        db.Suppliers.Remove(supplier);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
