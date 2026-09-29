using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications;
using NaijaPrimeSchool.Application.Communications.Dtos;
using NaijaPrimeSchool.Domain.Communications;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Notifications;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class NotificationPreferenceService(
    ApplicationDbContext db,
    ICurrentUser currentUser,
    IOptions<NotificationOptions> options) : INotificationPreferenceService
{
    public Task<NotificationPreferenceDto?> GetForCurrentUserAsync(CancellationToken ct = default) =>
        currentUser.UserId is { } userId ? LoadAsync(userId, ct) : Task.FromResult<NotificationPreferenceDto?>(null);

    public Task<OperationResult> SaveForCurrentUserAsync(UpdateNotificationPreferenceRequest request, CancellationToken ct = default) =>
        currentUser.UserId is { } userId
            ? SaveAsync(userId, request, ct)
            : Task.FromResult(OperationResult.Failure("Not signed in."));

    public async Task<NotificationPreferenceDto?> GetForUserAsync(Guid userId, CancellationToken ct = default) =>
        CanManageOthers() ? await LoadAsync(userId, ct) : null;

    public async Task<OperationResult> SaveForUserAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default) =>
        CanManageOthers()
            ? await SaveAsync(userId, request, ct)
            : OperationResult.Failure("You are not allowed to change another user's notification preferences.");

    private bool CanManageOthers() =>
        currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.HeadTeacher);

    private async Task<NotificationPreferenceDto?> LoadAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber })
            .FirstOrDefaultAsync(ct);
        if (user is null) return null;

        // Same fallbacks as NotificationService.ResolveRecipientsAsync, so the
        // page shows exactly where a reminder would be sent.
        var parent = await db.Parents
            .Where(p => p.UserId == userId)
            .Select(p => new { p.Email, p.PrimaryPhone, p.AlternatePhone })
            .FirstOrDefaultAsync(ct);

        var opts = options.Value;
        var pref = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        var effective = pref ?? new NotificationPreference();

        return new NotificationPreferenceDto
        {
            UserId = user.Id,
            DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
            EmailEnabled = effective.EmailEnabled,
            SmsEnabled = effective.SmsEnabled,
            QuietHoursEnabled = effective.QuietHoursEnabled,
            QuietHoursStart = effective.QuietHoursStart,
            QuietHoursEnd = effective.QuietHoursEnd,
            EmailDestination = parent is null
                ? ContactNormalizer.FirstEmail(user.Email)
                : ContactNormalizer.FirstEmail(parent.Email, user.Email),
            SmsDestination = parent is null
                ? ContactNormalizer.FirstPhone(user.PhoneNumber)
                : ContactNormalizer.FirstPhone(parent.PrimaryPhone, parent.AlternatePhone, user.PhoneNumber),
            SchoolEmailEnabled = opts.Email.Enabled,
            SchoolSmsEnabled = opts.Sms.Enabled,
            TimeZone = opts.TimeZone,
            IsSaved = pref is not null,
        };
    }

    private async Task<OperationResult> SaveAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct)
    {
        if (request.QuietHoursEnabled && request.QuietHoursStart == request.QuietHoursEnd)
            return OperationResult.Failure("Quiet hours must start and end at different times.");

        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
            return OperationResult.Failure("User not found.");

        var pref = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (pref is null)
        {
            pref = new NotificationPreference { UserId = userId };
            db.NotificationPreferences.Add(pref);
        }

        pref.EmailEnabled = request.EmailEnabled;
        pref.SmsEnabled = request.SmsEnabled;
        pref.QuietHoursEnabled = request.QuietHoursEnabled;
        pref.QuietHoursStart = request.QuietHoursStart;
        pref.QuietHoursEnd = request.QuietHoursEnd;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
