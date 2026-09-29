using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NaijaPrimeSchool.Application.Communications;
using NaijaPrimeSchool.Domain.Communications;
using NaijaPrimeSchool.Domain.Messaging;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

// Sends one batch of due Pending notifications. Scoped; the background
// worker creates a fresh scope (and therefore a fresh DbContext) per batch.
//
// Assumes a single app instance is dispatching. Running several web nodes
// against one database would need a row-claiming step (e.g. UPDATE ... OUTPUT
// with READPAST) to stop two nodes sending the same row.
public sealed class NotificationDispatcher(
    ApplicationDbContext db,
    IEmailGateway emailGateway,
    ISmsGateway smsGateway,
    IOptions<NotificationOptions> options,
    ILogger<NotificationDispatcher> logger)
{
    // Returns the number of rows processed (sent, failed, rescheduled or skipped).
    public async Task<int> DispatchDueAsync(CancellationToken ct = default)
    {
        var opts = options.Value;
        var statusIds = await db.NotificationStatuses.ToDictionaryAsync(s => s.Code, s => s.Id, ct);
        if (!statusIds.TryGetValue(NotificationCodes.Pending, out var pendingId))
            return 0;
        var channelCodes = await db.NotificationChannels.ToDictionaryAsync(c => c.Id, c => c.Code, ct);

        var now = DateTimeOffset.UtcNow;

        // IgnoreQueryFilters so rows whose announcement or recipient has been
        // soft-deleted still load (with the navigation populated) and can be
        // closed off as Skipped, instead of silently staying Pending forever.
        var due = await db.AnnouncementNotifications
            .IgnoreQueryFilters()
            .Include(n => n.Announcement).ThenInclude(a => a!.AnnouncementCategory)
            .Include(n => n.MessageThread)
            .Include(n => n.User)
            .Where(n => !n.IsDeleted && n.NotificationStatusId == pendingId && n.ScheduledFor <= now)
            .OrderBy(n => n.ScheduledFor)
            .Take(Math.Max(1, opts.BatchSize))
            .ToListAsync(ct);

        var userIds = due.Select(n => n.UserId).Distinct().ToList();
        var preferences = await db.NotificationPreferences
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);
        var zone = QuietHours.ResolveZone(opts.TimeZone);

        foreach (var n in due)
        {
            var channelCode = channelCodes.GetValueOrDefault(n.NotificationChannelId);
            var preference = preferences.GetValueOrDefault(n.UserId);
            var skipReason = await GetSkipReasonAsync(n, channelCode, preference, opts, ct);
            if (skipReason is not null)
            {
                n.NotificationStatusId = statusIds[NotificationCodes.Skipped];
                n.LastError = skipReason;
                await db.SaveChangesAsync(ct);
                continue;
            }

            // Inside the recipient's quiet hours: hold (not an attempt) and
            // let the read check run again when the window ends.
            // Message alerts are never urgent; announcements follow their category.
            if (NotificationPreferenceRules.HoldUntil(preference, n.Announcement?.AnnouncementCategory,
                    DateTimeOffset.UtcNow, zone) is { } resumeAt)
            {
                n.ScheduledFor = resumeAt;
                n.LastError = $"Held for quiet hours until {TimeZoneInfo.ConvertTime(resumeAt, zone):HH:mm}.";
                await db.SaveChangesAsync(ct);
                continue;
            }

            var result = await SendAsync(n, channelCode!, opts, zone, ct);

            n.AttemptCount++;
            n.LastAttemptOn = DateTimeOffset.UtcNow;
            if (result.Succeeded)
            {
                n.NotificationStatusId = statusIds[NotificationCodes.Sent];
                n.SentOn = n.LastAttemptOn;
                n.ProviderMessageId = Clip(result.ProviderMessageId, 200);
                n.LastError = null;
            }
            else
            {
                n.LastError = Clip(result.Error ?? "Unknown error.", 1000);
                if (n.AttemptCount >= Math.Max(1, opts.MaxAttempts))
                {
                    n.NotificationStatusId = statusIds[NotificationCodes.Failed];
                }
                else
                {
                    n.ScheduledFor = DateTimeOffset.UtcNow.AddMinutes(
                        Math.Max(1, opts.RetryDelayMinutes) * n.AttemptCount);
                }
                logger.LogWarning("Notification {Id} via {Channel} to {Destination} failed (attempt {Attempt}): {Error}",
                    n.Id, channelCode, n.Destination, n.AttemptCount, n.LastError);
            }

            // Save per row so a crash part-way through a batch never causes
            // an already-delivered message to be sent a second time.
            await db.SaveChangesAsync(ct);
        }

        return due.Count;
    }

    private async Task<string?> GetSkipReasonAsync(
        AnnouncementNotification n, string? channelCode, NotificationPreference? preference,
        NotificationOptions opts, CancellationToken ct)
    {
        if (n.MessageThreadId is not null)
        {
            if (n.MessageThread is null || n.MessageThread.IsDeleted) return "Conversation was deleted.";
        }
        else
        {
            var a = n.Announcement;
            if (a is null || a.IsDeleted) return "Announcement was deleted.";
            if (!a.IsPublished) return "Announcement was unpublished before the reminder went out.";
            if (a.ExpiresOn is { } exp && exp < DateOnly.FromDateTime(DateTime.UtcNow))
                return "Announcement expired before the reminder went out.";
        }

        if (n.User is null || n.User.IsDeleted || !n.User.IsActive)
            return "Recipient's portal account is inactive or deleted.";

        switch (channelCode)
        {
            case NotificationCodes.Email when !opts.Email.Enabled:
                return "Email notifications are switched off.";
            case NotificationCodes.Sms when !opts.Sms.Enabled:
                return "SMS notifications are switched off.";
            case NotificationCodes.Email or NotificationCodes.Sms:
                break;
            default:
                return $"Unknown notification channel '{channelCode}'.";
        }

        if (NotificationPreferenceRules.OptedOutReason(preference, channelCode) is { } optOut)
            return optOut;

        if (n.MessageThread is { } thread)
        {
            return (await UnreadOfficeMessagesAsync(thread, ct)).Count == 0
                ? "Read in the portal before the alert went out."
                : null;
        }

        var read = await db.AnnouncementReads.AnyAsync(
            r => r.AnnouncementId == n.AnnouncementId && r.UserId == n.UserId, ct);
        return read ? "Read in the portal before the reminder went out." : null;
    }

    // Office messages the family hasn't seen yet, oldest first.
    private async Task<List<ThreadMessage>> UnreadOfficeMessagesAsync(MessageThread thread, CancellationToken ct)
    {
        var seen = thread.FamilyLastReadOn;
        return await db.ThreadMessages
            .Include(m => m.SenderUser)
            .Where(m => m.MessageThreadId == thread.Id && m.IsFromStaff && (seen == null || m.SentOn > seen))
            .OrderBy(m => m.SentOn)
            .ToListAsync(ct);
    }

    private async Task<GatewayResult> SendAsync(
        AnnouncementNotification n, string channelCode, NotificationOptions opts, TimeZoneInfo zone, CancellationToken ct)
    {
        try
        {
            if (n.MessageThread is { } thread)
            {
                if (channelCode == NotificationCodes.Email)
                {
                    // Quote at most the last five unread office messages.
                    var unread = (await UnreadOfficeMessagesAsync(thread, ct))
                        .TakeLast(5)
                        .Select(m => ($"School office · {m.SenderUser?.FirstName} {m.SenderUser?.LastName}".TrimEnd(), m.SentOn, m.Body))
                        .ToList();
                    var mail = NotificationComposer.ComposeMessageEmail(opts, n.RecipientName, thread.Id, thread.Subject, unread, zone);
                    n.Subject = mail.Subject;
                    n.Message = Clip(mail.TextBody, 4000);
                    return await emailGateway.SendAsync(n.Destination, n.RecipientName, mail.Subject,
                        mail.TextBody, mail.HtmlBody, ct);
                }

                var alert = NotificationComposer.ComposeMessageSms(opts, thread.Id, thread.Subject);
                n.Subject = null;
                n.Message = alert;
                return await smsGateway.SendAsync(n.Destination, alert, ct);
            }

            var a = n.Announcement!;
            if (channelCode == NotificationCodes.Email)
            {
                var mail = NotificationComposer.ComposeEmail(opts, n.RecipientName, a.Title, a.Body);
                n.Subject = mail.Subject;
                n.Message = Clip(mail.TextBody, 4000);
                return await emailGateway.SendAsync(n.Destination, n.RecipientName, mail.Subject,
                    mail.TextBody, mail.HtmlBody, ct);
            }

            var text = NotificationComposer.ComposeSms(opts, a.Title, a.Body);
            n.Subject = null;
            n.Message = text;
            return await smsGateway.SendAsync(n.Destination, text, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GatewayResult.Failure(ex.Message);
        }
    }

    private static string? Clip(string? s, int max) =>
        s is null || s.Length <= max ? s : s[..max];
}
