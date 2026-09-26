using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications;
using NaijaPrimeSchool.Application.Communications.Dtos;
using NaijaPrimeSchool.Domain.Communications;
using NaijaPrimeSchool.Infrastructure.Notifications;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class NotificationService(
    ApplicationDbContext db,
    IOptions<NotificationOptions> options) : INotificationService
{
    private sealed record Recipient(Guid UserId, string Name, string? Email, string? Phone);

    public async Task<OperationResult<NotificationQueueResult>> QueueForAnnouncementAsync(
        Guid announcementId, bool sendNow, CancellationToken ct = default)
    {
        var a = await db.Announcements
            .Include(x => x.AnnouncementAudience)
            .FirstOrDefaultAsync(x => x.Id == announcementId, ct);
        if (a is null) return OperationResult<NotificationQueueResult>.Failure("Announcement not found.");
        if (!a.IsPublished)
            return OperationResult<NotificationQueueResult>.Failure("Publish the announcement before notifying families.");
        if (a.ExpiresOn is { } exp && exp < DateOnly.FromDateTime(DateTime.UtcNow))
            return OperationResult<NotificationQueueResult>.Failure("This announcement has expired.");

        var opts = options.Value;
        var channels = await db.NotificationChannels
            .Where(c => (c.Code == NotificationCodes.Email && opts.Email.Enabled)
                        || (c.Code == NotificationCodes.Sms && opts.Sms.Enabled))
            .ToListAsync(ct);
        if (channels.Count == 0)
            return OperationResult<NotificationQueueResult>.Failure(
                "Email and SMS notifications are both switched off in configuration.");

        var statusIds = await db.NotificationStatuses.ToDictionaryAsync(s => s.Code, s => s.Id, ct);
        var pendingId = statusIds[NotificationCodes.Pending];
        var sentId = statusIds[NotificationCodes.Sent];

        var recipients = await ResolveRecipientsAsync(a, ct);
        var readers = (await db.AnnouncementReads
                .Where(r => r.AnnouncementId == announcementId)
                .Select(r => r.UserId)
                .ToListAsync(ct))
            .ToHashSet();
        var existing = (await db.AnnouncementNotifications
                .Where(n => n.AnnouncementId == announcementId)
                .ToListAsync(ct))
            .ToDictionary(n => (n.UserId, n.NotificationChannelId));

        var now = DateTimeOffset.UtcNow;
        var graceEnds = (a.PublishedOn ?? now).AddMinutes(Math.Max(0, opts.UnreadGraceMinutes));
        var scheduledFor = sendNow || graceEnds < now ? now : graceEnds;

        var skippedId = statusIds[NotificationCodes.Skipped];
        var result = new NotificationQueueResult { Recipients = recipients.Count };
        foreach (var r in recipients)
        {
            if (readers.Contains(r.UserId))
            {
                // Close off reminders still waiting out the grace period now,
                // rather than leaving them Pending until the dispatcher runs.
                foreach (var channel in channels)
                {
                    if (existing.TryGetValue((r.UserId, channel.Id), out var waiting)
                        && waiting.NotificationStatusId == pendingId)
                    {
                        waiting.NotificationStatusId = skippedId;
                        waiting.LastError = "Read in the portal before the reminder went out.";
                    }
                }
                result.AlreadyRead++;
                continue;
            }

            foreach (var channel in channels)
            {
                var destination = channel.Code == NotificationCodes.Email ? r.Email : r.Phone;
                if (destination is null)
                {
                    result.MissingContact++;
                    continue;
                }

                if (existing.TryGetValue((r.UserId, channel.Id), out var row))
                {
                    if (row.NotificationStatusId == pendingId || row.NotificationStatusId == sentId)
                    {
                        // "Notify now" pulls a row still waiting out its grace
                        // period forward instead of creating a duplicate.
                        if (sendNow && row.NotificationStatusId == pendingId && row.ScheduledFor > now)
                            row.ScheduledFor = now;
                        result.AlreadyQueued++;
                        continue;
                    }

                    // Failed or Skipped: re-arm the same row.
                    row.NotificationStatusId = pendingId;
                    row.ScheduledFor = scheduledFor;
                    row.AttemptCount = 0;
                    row.LastError = null;
                    row.RecipientName = r.Name;
                    row.Destination = destination;
                    result.Queued++;
                    continue;
                }

                db.AnnouncementNotifications.Add(new AnnouncementNotification
                {
                    AnnouncementId = a.Id,
                    UserId = r.UserId,
                    NotificationChannelId = channel.Id,
                    NotificationStatusId = pendingId,
                    RecipientName = r.Name,
                    Destination = destination,
                    ScheduledFor = scheduledFor,
                });
                result.Queued++;
            }
        }

        await db.SaveChangesAsync(ct);
        return OperationResult<NotificationQueueResult>.Success(result);
    }

    public async Task<IReadOnlyList<AnnouncementNotificationDto>> ListAsync(NotificationFilter filter, CancellationToken ct = default)
    {
        var q = db.AnnouncementNotifications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(n =>
                n.RecipientName.ToLower().Contains(term)
                || n.Destination.ToLower().Contains(term)
                || n.Announcement!.Title.ToLower().Contains(term));
        }

        if (filter.AnnouncementId is { } aid) q = q.Where(n => n.AnnouncementId == aid);
        if (filter.ChannelId is { } cid) q = q.Where(n => n.NotificationChannelId == cid);
        if (filter.StatusId is { } sid) q = q.Where(n => n.NotificationStatusId == sid);

        return await q
            .OrderByDescending(n => n.LastAttemptOn ?? n.ScheduledFor)
            .Take(Math.Clamp(filter.Take, 1, 5000))
            .Select(n => new AnnouncementNotificationDto
            {
                Id = n.Id,
                AnnouncementId = n.AnnouncementId,
                AnnouncementTitle = n.Announcement!.Title,
                UserId = n.UserId,
                RecipientName = n.RecipientName,
                Destination = n.Destination,
                ChannelId = n.NotificationChannelId,
                ChannelName = n.NotificationChannel!.Name,
                ChannelCode = n.NotificationChannel.Code,
                StatusId = n.NotificationStatusId,
                StatusName = n.NotificationStatus!.Name,
                StatusCode = n.NotificationStatus.Code,
                ScheduledFor = n.ScheduledFor,
                AttemptCount = n.AttemptCount,
                LastAttemptOn = n.LastAttemptOn,
                SentOn = n.SentOn,
                LastError = n.LastError,
                Subject = n.Subject,
                Message = n.Message,
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, NotificationCounts>> GetCountsByAnnouncementAsync(
        IReadOnlyCollection<Guid> announcementIds, CancellationToken ct = default)
    {
        if (announcementIds.Count == 0) return new Dictionary<Guid, NotificationCounts>();

        var rows = await db.AnnouncementNotifications
            .Where(n => announcementIds.Contains(n.AnnouncementId))
            .GroupBy(n => new { n.AnnouncementId, n.NotificationStatus!.Code })
            .Select(g => new { g.Key.AnnouncementId, g.Key.Code, Count = g.Count() })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, NotificationCounts>();
        foreach (var row in rows)
        {
            if (!result.TryGetValue(row.AnnouncementId, out var counts))
                result[row.AnnouncementId] = counts = new NotificationCounts();

            switch (row.Code)
            {
                case NotificationCodes.Pending: counts.Pending = row.Count; break;
                case NotificationCodes.Sent: counts.Sent = row.Count; break;
                case NotificationCodes.Failed: counts.Failed = row.Count; break;
                case NotificationCodes.Skipped: counts.Skipped = row.Count; break;
            }
        }
        return result;
    }

    public async Task<OperationResult> RetryAsync(Guid notificationId, CancellationToken ct = default)
    {
        var n = await db.AnnouncementNotifications
            .Include(x => x.NotificationStatus)
            .FirstOrDefaultAsync(x => x.Id == notificationId, ct);
        if (n is null) return OperationResult.Failure("Notification not found.");
        if (n.NotificationStatus!.Code == NotificationCodes.Sent)
            return OperationResult.Failure("This notification has already been sent.");

        n.NotificationStatusId = await StatusIdAsync(NotificationCodes.Pending, ct);
        n.ScheduledFor = DateTimeOffset.UtcNow;
        n.AttemptCount = 0;
        n.LastError = null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult<int>> RetryAllFailedAsync(Guid? announcementId = null, CancellationToken ct = default)
    {
        var failedId = await StatusIdAsync(NotificationCodes.Failed, ct);
        var pendingId = await StatusIdAsync(NotificationCodes.Pending, ct);

        var q = db.AnnouncementNotifications.Where(n => n.NotificationStatusId == failedId);
        if (announcementId is { } aid) q = q.Where(n => n.AnnouncementId == aid);
        var rows = await q.ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var n in rows)
        {
            n.NotificationStatusId = pendingId;
            n.ScheduledFor = now;
            n.AttemptCount = 0;
            n.LastError = null;
        }
        await db.SaveChangesAsync(ct);
        return OperationResult<int>.Success(rows.Count);
    }

    // Mirrors the portal visibility rules in AnnouncementService: ALL reaches
    // parents and students, PARENT / STUDENT reach one side, and a
    // class-targeted audience reaches pupils actively enrolled in the class
    // plus every parent linked to one of them. Only people with an active
    // portal account are included, since the nudge points at the portal.
    private async Task<List<Recipient>> ResolveRecipientsAsync(Announcement a, CancellationToken ct)
    {
        var audience = a.AnnouncementAudience!;
        Guid? classId = null;
        if (audience.RequiresTargetClass)
        {
            if (a.TargetSchoolClassId is null) return [];
            classId = a.TargetSchoolClassId;
        }

        var includeParents = classId is not null || audience.Code is "ALL" or "PARENT";
        var includeStudents = classId is not null || audience.Code is "ALL" or "STUDENT";

        var recipients = new List<Recipient>();

        if (includeParents)
        {
            var q = db.Parents.Where(p => p.IsActive && p.UserId != null && p.User!.IsActive);
            if (classId is { } cls)
            {
                q = q.Where(p => p.StudentLinks.Any(l =>
                    l.Student!.IsActive
                    && l.Student.Enrolments.Any(e => e.WithdrawnOn == null && e.SchoolClassId == cls)));
            }

            var parents = await q
                .Select(p => new
                {
                    UserId = p.UserId!.Value,
                    p.FirstName,
                    p.LastName,
                    p.Email,
                    p.PrimaryPhone,
                    p.AlternatePhone,
                    UserEmail = p.User!.Email,
                    UserPhone = p.User.PhoneNumber,
                })
                .ToListAsync(ct);

            recipients.AddRange(parents.Select(p => new Recipient(
                p.UserId,
                $"{p.FirstName} {p.LastName}".Trim(),
                ContactNormalizer.NormalizeEmail(p.Email) ?? ContactNormalizer.NormalizeEmail(p.UserEmail),
                ContactNormalizer.NormalizePhone(p.PrimaryPhone)
                    ?? ContactNormalizer.NormalizePhone(p.AlternatePhone)
                    ?? ContactNormalizer.NormalizePhone(p.UserPhone))));
        }

        if (includeStudents)
        {
            var q = db.Students.Where(s => s.IsActive && s.UserId != null && s.User!.IsActive);
            if (classId is { } cls)
                q = q.Where(s => s.Enrolments.Any(e => e.WithdrawnOn == null && e.SchoolClassId == cls));

            var students = await q
                .Select(s => new
                {
                    UserId = s.UserId!.Value,
                    s.FirstName,
                    s.LastName,
                    UserEmail = s.User!.Email,
                    UserPhone = s.User.PhoneNumber,
                })
                .ToListAsync(ct);

            recipients.AddRange(students.Select(s => new Recipient(
                s.UserId,
                $"{s.FirstName} {s.LastName}".Trim(),
                ContactNormalizer.NormalizeEmail(s.UserEmail),
                ContactNormalizer.NormalizePhone(s.UserPhone))));
        }

        return recipients.DistinctBy(r => r.UserId).ToList();
    }

    private Task<Guid> StatusIdAsync(string code, CancellationToken ct) =>
        db.NotificationStatuses.Where(s => s.Code == code).Select(s => s.Id).FirstAsync(ct);
}
