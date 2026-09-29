using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications.Dtos;

namespace NaijaPrimeSchool.Application.Communications;

// Per-user email / SMS opt-outs and quiet hours for unread-announcement
// reminders. The *ForCurrentUser methods back the portal page; the
// *ForUser methods let SuperAdmin / HeadTeacher set them on a family's
// behalf (e.g. a parent without a smartphone who phones the office).
public interface INotificationPreferenceService
{
    Task<NotificationPreferenceDto?> GetForCurrentUserAsync(CancellationToken ct = default);
    Task<OperationResult> SaveForCurrentUserAsync(UpdateNotificationPreferenceRequest request, CancellationToken ct = default);

    Task<NotificationPreferenceDto?> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<OperationResult> SaveForUserAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default);
}
