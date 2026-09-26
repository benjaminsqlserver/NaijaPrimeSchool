using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications.Dtos;

namespace NaijaPrimeSchool.Application.Communications;

// Email / SMS nudges for announcements a parent or student has not yet read
// in the portal. Queueing only creates AnnouncementNotification rows; the
// background dispatcher does the actual sending.
public interface INotificationService
{
    // Queues one row per (recipient, enabled channel) for a published
    // announcement. With sendNow = false the rows wait for the configured
    // unread grace period, giving families a chance to read the notice in
    // the portal first; sendNow = true schedules them immediately.
    Task<OperationResult<NotificationQueueResult>> QueueForAnnouncementAsync(
        Guid announcementId, bool sendNow, CancellationToken ct = default);

    Task<IReadOnlyList<AnnouncementNotificationDto>> ListAsync(NotificationFilter filter, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, NotificationCounts>> GetCountsByAnnouncementAsync(
        IReadOnlyCollection<Guid> announcementIds, CancellationToken ct = default);

    Task<OperationResult> RetryAsync(Guid notificationId, CancellationToken ct = default);
    Task<OperationResult<int>> RetryAllFailedAsync(Guid? announcementId = null, CancellationToken ct = default);
}
