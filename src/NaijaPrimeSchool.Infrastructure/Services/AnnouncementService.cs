using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications;
using NaijaPrimeSchool.Application.Communications.Dtos;
using NaijaPrimeSchool.Domain.Communications;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class AnnouncementService(
    ApplicationDbContext db,
    ICurrentUser currentUser) : IAnnouncementService
{
    public async Task<IReadOnlyList<AnnouncementDto>> ListAsync(AnnouncementFilter filter, CancellationToken ct = default)
    {
        var q = db.Announcements
            .Include(a => a.AnnouncementCategory)
            .Include(a => a.AnnouncementAudience)
            .Include(a => a.TargetSchoolClass)
            .Include(a => a.PostedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(a =>
                a.Title.ToLower().Contains(term)
                || a.Body.ToLower().Contains(term));
        }

        if (filter.CategoryId is { } cat) q = q.Where(a => a.AnnouncementCategoryId == cat);
        if (filter.AudienceId is { } aud) q = q.Where(a => a.AnnouncementAudienceId == aud);
        if (filter.TargetSchoolClassId is { } cls) q = q.Where(a => a.TargetSchoolClassId == cls);
        if (filter.IsPublished is { } pub) q = q.Where(a => a.IsPublished == pub);

        if (!filter.IncludeExpired)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            q = q.Where(a => a.ExpiresOn == null || a.ExpiresOn >= today);
        }

        var rows = await q
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedOn ?? a.CreatedOn)
            .ToListAsync(ct);

        var ids = rows.Select(r => r.Id).ToList();
        var reads = await db.AnnouncementReads
            .Where(r => ids.Contains(r.AnnouncementId))
            .GroupBy(r => r.AnnouncementId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var myReads = currentUser.UserId is { } myId
            ? await db.AnnouncementReads
                .Where(r => r.UserId == myId && ids.Contains(r.AnnouncementId))
                .Select(r => r.AnnouncementId)
                .ToListAsync(ct)
            : [];

        return rows.Select(a => MapDto(a, reads.GetValueOrDefault(a.Id), myReads.Contains(a.Id))).ToList();
    }

    public async Task<AnnouncementDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.Announcements
            .Include(x => x.AnnouncementCategory)
            .Include(x => x.AnnouncementAudience)
            .Include(x => x.TargetSchoolClass)
            .Include(x => x.PostedBy)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return null;

        var readCount = await db.AnnouncementReads.CountAsync(r => r.AnnouncementId == id, ct);
        var readByMe = currentUser.UserId is { } myId
            && await db.AnnouncementReads.AnyAsync(r => r.AnnouncementId == id && r.UserId == myId, ct);

        return MapDto(a, readCount, readByMe);
    }

    public async Task<OperationResult<Guid>> CreateAsync(CreateAnnouncementRequest request, CancellationToken ct = default)
    {
        var errors = await ValidateAsync(request.Title, request.Body, request.CategoryId, request.AudienceId,
            request.TargetSchoolClassId, ct);
        if (errors.Count > 0) return OperationResult<Guid>.Failure(errors);

        var a = new Announcement
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            AnnouncementCategoryId = request.CategoryId,
            AnnouncementAudienceId = request.AudienceId,
            TargetSchoolClassId = request.TargetSchoolClassId,
            ExpiresOn = request.ExpiresOn,
            IsPinned = request.IsPinned,
            PostedById = currentUser.UserId,
            IsPublished = request.PublishImmediately,
            PublishedOn = request.PublishImmediately ? DateTimeOffset.UtcNow : null,
        };
        db.Announcements.Add(a);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(a.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateAnnouncementRequest request, CancellationToken ct = default)
    {
        var a = await db.Announcements.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (a is null) return OperationResult.Failure("Announcement not found.");

        var errors = await ValidateAsync(request.Title, request.Body, request.CategoryId, request.AudienceId,
            request.TargetSchoolClassId, ct);
        if (errors.Count > 0) return OperationResult.Failure(errors);

        a.Title = request.Title.Trim();
        a.Body = request.Body.Trim();
        a.AnnouncementCategoryId = request.CategoryId;
        a.AnnouncementAudienceId = request.AudienceId;
        a.TargetSchoolClassId = request.TargetSchoolClassId;
        a.ExpiresOn = request.ExpiresOn;
        a.IsPinned = request.IsPinned;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.Announcements.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return OperationResult.Failure("Announcement not found.");
        if (a.IsPublished) return OperationResult.Success();
        a.IsPublished = true;
        a.PublishedOn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UnpublishAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.Announcements.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return OperationResult.Failure("Announcement not found.");
        if (!a.IsPublished) return OperationResult.Success();
        a.IsPublished = false;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.Announcements.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return OperationResult.Failure("Announcement not found.");
        db.Announcements.Remove(a);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<AnnouncementDto>> ListForCurrentUserAsync(int take = 20, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId)
            return [];

        var audienceCodes = await ResolveAudienceCodesForCurrentUserAsync(ct);
        var classIds = await ResolveRelevantClassIdsForCurrentUserAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var q = db.Announcements
            .Include(a => a.AnnouncementCategory)
            .Include(a => a.AnnouncementAudience)
            .Include(a => a.TargetSchoolClass)
            .Include(a => a.PostedBy)
            .Where(a => a.IsPublished)
            .Where(a => a.ExpiresOn == null || a.ExpiresOn >= today)
            .Where(a =>
                audienceCodes.Contains(a.AnnouncementAudience!.Code)
                || (a.AnnouncementAudience.RequiresTargetClass
                    && a.TargetSchoolClassId != null
                    && classIds.Contains(a.TargetSchoolClassId.Value)));

        var rows = await q
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedOn ?? a.CreatedOn)
            .Take(take)
            .ToListAsync(ct);

        var ids = rows.Select(r => r.Id).ToList();
        var myReads = await db.AnnouncementReads
            .Where(r => r.UserId == userId && ids.Contains(r.AnnouncementId))
            .Select(r => r.AnnouncementId)
            .ToListAsync(ct);

        return rows.Select(a => MapDto(a, readCount: 0, readByMe: myReads.Contains(a.Id))).ToList();
    }

    public async Task<OperationResult> MarkAsReadAsync(Guid announcementId, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId)
            return OperationResult.Failure("Not signed in.");

        var exists = await db.AnnouncementReads.AnyAsync(
            r => r.AnnouncementId == announcementId && r.UserId == userId, ct);
        if (exists) return OperationResult.Success();

        db.AnnouncementReads.Add(new AnnouncementRead
        {
            AnnouncementId = announcementId,
            UserId = userId,
            ReadOn = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<int> CountUnreadForCurrentUserAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } userId)
            return 0;

        var audienceCodes = await ResolveAudienceCodesForCurrentUserAsync(ct);
        var classIds = await ResolveRelevantClassIdsForCurrentUserAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var visible = db.Announcements
            .Where(a => a.IsPublished)
            .Where(a => a.ExpiresOn == null || a.ExpiresOn >= today)
            .Where(a =>
                audienceCodes.Contains(a.AnnouncementAudience!.Code)
                || (a.AnnouncementAudience.RequiresTargetClass
                    && a.TargetSchoolClassId != null
                    && classIds.Contains(a.TargetSchoolClassId.Value)));

        var unread = visible
            .Where(a => !db.AnnouncementReads.Any(r => r.AnnouncementId == a.Id && r.UserId == userId));

        return await unread.CountAsync(ct);
    }

    private async Task<IReadOnlyList<string>> ResolveAudienceCodesForCurrentUserAsync(CancellationToken ct)
    {
        // Everyone always sees ALL announcements; parents and students get
        // their respective audience-coded ones. SuperAdmin / HeadTeacher see
        // every audience here too (so the admin preview matches reality).
        var codes = new List<string> { "ALL" };
        if (currentUser.IsInRole(Roles.Parent) || currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.HeadTeacher))
            codes.Add("PARENT");
        if (currentUser.IsInRole(Roles.Student) || currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.HeadTeacher))
            codes.Add("STUDENT");
        await Task.CompletedTask;
        return codes;
    }

    private async Task<IReadOnlyList<Guid>> ResolveRelevantClassIdsForCurrentUserAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not { } userId) return [];

        // A parent sees announcements for any class one of their wards is
        // actively enrolled in; a student sees their own current class.
        var studentClassIds = await db.Students
            .Where(s => s.UserId == userId)
            .Select(s => s.Enrolments
                .Where(e => e.WithdrawnOn == null)
                .OrderByDescending(e => e.EnrolledOn)
                .Select(e => (Guid?)e.SchoolClassId)
                .FirstOrDefault())
            .Where(id => id != null)
            .Select(id => id!.Value)
            .ToListAsync(ct);

        var wardClassIds = await db.StudentParents
            .Where(sp => sp.Parent!.UserId == userId)
            .SelectMany(sp => sp.Student!.Enrolments
                .Where(e => e.WithdrawnOn == null))
            .Select(e => e.SchoolClassId)
            .ToListAsync(ct);

        return studentClassIds.Concat(wardClassIds).Distinct().ToList();
    }

    private async Task<IReadOnlyList<string>> ValidateAsync(
        string title, string body, Guid categoryId, Guid audienceId, Guid? targetClassId, CancellationToken ct)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(title)) errors.Add("Title is required.");
        if (string.IsNullOrWhiteSpace(body)) errors.Add("Body is required.");

        if (!await db.AnnouncementCategories.AnyAsync(c => c.Id == categoryId, ct))
            errors.Add("Category not found.");

        var audience = await db.AnnouncementAudiences.FirstOrDefaultAsync(a => a.Id == audienceId, ct);
        if (audience is null)
        {
            errors.Add("Audience not found.");
        }
        else if (audience.RequiresTargetClass)
        {
            if (targetClassId is null)
                errors.Add($"Audience '{audience.Name}' requires a target class.");
            else if (!await db.SchoolClasses.AnyAsync(c => c.Id == targetClassId, ct))
                errors.Add("Target class not found.");
        }
        else if (targetClassId is not null)
        {
            errors.Add($"Audience '{audience.Name}' is broadcast, drop the target class.");
        }

        return errors;
    }

    private static AnnouncementDto MapDto(Announcement a, int readCount, bool readByMe) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Body = a.Body,
        CategoryId = a.AnnouncementCategoryId,
        CategoryName = a.AnnouncementCategory?.Name ?? string.Empty,
        CategoryCode = a.AnnouncementCategory?.Code ?? string.Empty,
        AudienceId = a.AnnouncementAudienceId,
        AudienceName = a.AnnouncementAudience?.Name ?? string.Empty,
        AudienceCode = a.AnnouncementAudience?.Code ?? string.Empty,
        AudienceRequiresTargetClass = a.AnnouncementAudience?.RequiresTargetClass ?? false,
        TargetSchoolClassId = a.TargetSchoolClassId,
        TargetSchoolClassName = a.TargetSchoolClass?.Name,
        PostedById = a.PostedById,
        PostedByName = a.PostedBy is null ? null : $"{a.PostedBy.FirstName} {a.PostedBy.LastName}".Trim(),
        IsPublished = a.IsPublished,
        PublishedOn = a.PublishedOn,
        ExpiresOn = a.ExpiresOn,
        IsPinned = a.IsPinned,
        CreatedOn = a.CreatedOn,
        CreatedBy = a.CreatedBy,
        ModifiedOn = a.ModifiedOn,
        ModifiedBy = a.ModifiedBy,
        ReadCount = readCount,
        ReadByCurrentUser = readByMe,
    };
}
