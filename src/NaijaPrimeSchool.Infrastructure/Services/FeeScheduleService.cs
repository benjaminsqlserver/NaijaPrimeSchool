using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance;
using NaijaPrimeSchool.Application.Finance.Dtos;
using NaijaPrimeSchool.Domain.Finance;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class FeeScheduleService(ApplicationDbContext db) : IFeeScheduleService
{
    private IQueryable<FeeScheduleDto> Project(IQueryable<FeeSchedule> q) =>
        q.Select(s => new FeeScheduleDto
        {
            Id = s.Id,
            TermId = s.TermId,
            TermName = s.Term!.TermType!.Name + " — " + s.Term!.Session!.Name,
            SessionId = s.Term!.SessionId,
            SessionName = s.Term!.Session!.Name,
            ClassLevelId = s.ClassLevelId,
            ClassLevelName = s.ClassLevel!.Name,
            Title = s.Title,
            Notes = s.Notes,
            IsPublished = s.IsPublished,
            PublishedOn = s.PublishedOn,
            ItemCount = s.Items.Count,
            TotalAmount = s.Items.Sum(i => (decimal?)i.Amount) ?? 0m,
            InvoicesIssued = db.InvoiceLines
                .Count(l => l.FeeScheduleItem != null
                    && l.FeeScheduleItem.FeeScheduleId == s.Id),
        });

    public async Task<IReadOnlyList<FeeScheduleDto>> ListAsync(FeeScheduleFilter filter, CancellationToken ct = default)
    {
        var q = db.FeeSchedules.AsQueryable();
        if (filter.TermId.HasValue) q = q.Where(s => s.TermId == filter.TermId.Value);
        if (filter.ClassLevelId.HasValue) q = q.Where(s => s.ClassLevelId == filter.ClassLevelId.Value);
        if (filter.IsPublished.HasValue) q = q.Where(s => s.IsPublished == filter.IsPublished.Value);

        return await Project(q
                .OrderBy(s => s.ClassLevel!.DisplayOrder)
                .ThenBy(s => s.Title))
            .ToListAsync(ct);
    }

    public async Task<FeeScheduleDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await Project(db.FeeSchedules.Where(s => s.Id == id))
            .FirstOrDefaultAsync(ct);
        if (schedule is null) return null;

        var items = await db.FeeScheduleItems
            .Where(i => i.FeeScheduleId == id)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.FeeCategory!.DisplayOrder)
            .Select(i => new FeeScheduleItemDto
            {
                Id = i.Id,
                FeeScheduleId = i.FeeScheduleId,
                FeeCategoryId = i.FeeCategoryId,
                FeeCategoryName = i.FeeCategory!.Name,
                FeeCategoryCode = i.FeeCategory!.Code,
                Description = i.Description,
                Amount = i.Amount,
                IsMandatory = i.IsMandatory,
                DisplayOrder = i.DisplayOrder,
            })
            .ToListAsync(ct);

        return new FeeScheduleDetailDto { Schedule = schedule, Items = items };
    }

    public async Task<OperationResult<Guid>> CreateAsync(CreateFeeScheduleRequest request, CancellationToken ct = default)
    {
        if (!await db.Terms.AnyAsync(t => t.Id == request.TermId, ct))
            return OperationResult<Guid>.Failure("Term not found.");

        if (!await db.ClassLevels.AnyAsync(l => l.Id == request.ClassLevelId, ct))
            return OperationResult<Guid>.Failure("Class level not found.");

        if (await db.FeeSchedules.AnyAsync(s =>
                s.TermId == request.TermId && s.ClassLevelId == request.ClassLevelId, ct))
            return OperationResult<Guid>.Failure(
                "A fee schedule already exists for this (term, class level). Edit that one instead.");

        var schedule = new FeeSchedule
        {
            TermId = request.TermId,
            ClassLevelId = request.ClassLevelId,
            Title = request.Title.Trim(),
            Notes = request.Notes,
        };
        db.FeeSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(schedule.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateFeeScheduleRequest request, CancellationToken ct = default)
    {
        var schedule = await db.FeeSchedules.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (schedule is null) return OperationResult.Failure("Fee schedule not found.");
        if (schedule.IsPublished) return OperationResult.Failure(
            "Unpublish the fee schedule before editing.");

        schedule.Title = request.Title.Trim();
        schedule.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await db.FeeSchedules
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (schedule is null) return OperationResult.Failure("Fee schedule not found.");
        if (schedule.Items.Count == 0)
            return OperationResult.Failure(
                "Add at least one line item before publishing the schedule.");

        schedule.IsPublished = true;
        schedule.PublishedOn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UnpublishAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await db.FeeSchedules.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (schedule is null) return OperationResult.Failure("Fee schedule not found.");

        var hasInvoices = await db.InvoiceLines
            .AnyAsync(l => l.FeeScheduleItem != null
                && l.FeeScheduleItem.FeeScheduleId == id, ct);
        if (hasInvoices)
            return OperationResult.Failure(
                "Invoices have already been issued from this schedule. Cancel the affected invoices first.");

        schedule.IsPublished = false;
        schedule.PublishedOn = null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await db.FeeSchedules
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (schedule is null) return OperationResult.Failure("Fee schedule not found.");

        var hasInvoices = await db.InvoiceLines
            .AnyAsync(l => l.FeeScheduleItem != null
                && l.FeeScheduleItem.FeeScheduleId == id, ct);
        if (hasInvoices)
            return OperationResult.Failure(
                "Cannot delete a schedule that has been used to issue invoices.");

        db.FeeSchedules.Remove(schedule);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult<Guid>> UpsertItemAsync(UpsertFeeScheduleItemRequest request, CancellationToken ct = default)
    {
        var schedule = await db.FeeSchedules.FirstOrDefaultAsync(s => s.Id == request.FeeScheduleId, ct);
        if (schedule is null)
            return OperationResult<Guid>.Failure("Fee schedule not found.");
        if (schedule.IsPublished)
            return OperationResult<Guid>.Failure("Unpublish the schedule before editing items.");

        if (!await db.FeeCategories.AnyAsync(c => c.Id == request.FeeCategoryId, ct))
            return OperationResult<Guid>.Failure("Fee category not found.");

        if (request.Amount <= 0)
            return OperationResult<Guid>.Failure("Amount must be greater than zero.");

        FeeScheduleItem item;
        if (request.Id.HasValue)
        {
            item = await db.FeeScheduleItems.FirstOrDefaultAsync(i => i.Id == request.Id.Value, ct)
                   ?? throw new InvalidOperationException("Item not found.");
            item.FeeCategoryId = request.FeeCategoryId;
            item.Description = request.Description.Trim();
            item.Amount = request.Amount;
            item.IsMandatory = request.IsMandatory;
            item.DisplayOrder = request.DisplayOrder;
        }
        else
        {
            item = new FeeScheduleItem
            {
                FeeScheduleId = request.FeeScheduleId,
                FeeCategoryId = request.FeeCategoryId,
                Description = request.Description.Trim(),
                Amount = request.Amount,
                IsMandatory = request.IsMandatory,
                DisplayOrder = request.DisplayOrder,
            };
            db.FeeScheduleItems.Add(item);
        }

        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(item.Id);
    }

    public async Task<OperationResult> RemoveItemAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await db.FeeScheduleItems
            .Include(i => i.FeeSchedule)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null) return OperationResult.Failure("Item not found.");
        if (item.FeeSchedule!.IsPublished)
            return OperationResult.Failure("Unpublish the schedule before removing items.");

        db.FeeScheduleItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
