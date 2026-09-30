using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Auditing;
using NaijaPrimeSchool.Application.Auditing.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class AuditLogService(ApplicationDbContext db, ICurrentUser currentUser) : IAuditLogService
{
    private bool CanView => currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.HeadTeacher);

    public async Task<AuditLogPage> ListAsync(AuditLogFilter filter, CancellationToken ct = default)
    {
        if (!CanView) return new AuditLogPage();

        var q = db.AuditEntries.AsNoTracking().AsQueryable();

        // Dates are whole days, inclusive, in UTC.
        if (filter.From is { } from)
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(e => e.OccurredOn >= start);
        }
        if (filter.To is { } to)
        {
            var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(e => e.OccurredOn < end);
        }
        if (!string.IsNullOrWhiteSpace(filter.UserName)) q = q.Where(e => e.UserName == filter.UserName);
        if (!string.IsNullOrWhiteSpace(filter.EntityType)) q = q.Where(e => e.EntityType == filter.EntityType);
        if (!string.IsNullOrWhiteSpace(filter.EntityId)) q = q.Where(e => e.EntityId == filter.EntityId.Trim());
        if (!string.IsNullOrWhiteSpace(filter.ActionCode)) q = q.Where(e => e.AuditAction!.Code == filter.ActionCode);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            q = q.Where(e =>
                (e.EntityLabel != null && e.EntityLabel.Contains(term))
                || e.EntityId.Contains(term)
                || e.Changes.Contains(term));
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(e => e.OccurredOn)
            .ThenBy(e => e.EntityType)
            .Skip(Math.Max(0, filter.Skip))
            .Take(Math.Clamp(filter.Take, 1, 500))
            .Select(e => new
            {
                e.Id, e.OccurredOn, e.UserId, e.UserName, e.EntityType, e.EntityId, e.EntityLabel, e.Changes,
                ActionCode = e.AuditAction!.Code,
                ActionName = e.AuditAction.Name,
            })
            .ToListAsync(ct);

        return new AuditLogPage
        {
            TotalCount = total,
            Items = rows.Select(r => new AuditEntryDto
            {
                Id = r.Id,
                OccurredOn = r.OccurredOn,
                UserId = r.UserId,
                UserName = r.UserName,
                ActionCode = r.ActionCode,
                ActionName = r.ActionName,
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                EntityLabel = r.EntityLabel,
                Changes = ParseChanges(r.Changes),
            }).ToList(),
        };
    }

    public async Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default) =>
        CanView
            ? await db.AuditEntries.Select(e => e.EntityType).Distinct().OrderBy(t => t).ToListAsync(ct)
            : [];

    public async Task<IReadOnlyList<string>> GetUserNamesAsync(CancellationToken ct = default) =>
        CanView
            ? await db.AuditEntries.Select(e => e.UserName).Distinct().OrderBy(u => u).ToListAsync(ct)
            : [];

    private static IReadOnlyList<AuditFieldChangeDto> ParseChanges(string json)
    {
        try { return JsonSerializer.Deserialize<List<AuditFieldChangeDto>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
