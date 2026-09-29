using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

internal sealed record RecipientContact(Guid UserId, string Name, string? Email, string? Phone);

internal static class RecipientContacts
{
    // Where a notification for one portal user goes: a parent's own record
    // first (Email; PrimaryPhone, then AlternatePhone), falling back to the
    // portal account; students use their portal account. The bulk version
    // for announcements lives in NotificationService.ResolveRecipientsAsync
    // and applies the same fallbacks.
    public static async Task<RecipientContact?> ForUserAsync(ApplicationDbContext db, Guid userId, CancellationToken ct)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber })
            .FirstOrDefaultAsync(ct);
        if (user is null) return null;

        var parent = await db.Parents
            .Where(p => p.UserId == userId)
            .Select(p => new { p.FirstName, p.LastName, p.Email, p.PrimaryPhone, p.AlternatePhone })
            .FirstOrDefaultAsync(ct);

        return parent is null
            ? new RecipientContact(user.Id, $"{user.FirstName} {user.LastName}".Trim(),
                ContactNormalizer.FirstEmail(user.Email),
                ContactNormalizer.FirstPhone(user.PhoneNumber))
            : new RecipientContact(user.Id, $"{parent.FirstName} {parent.LastName}".Trim(),
                ContactNormalizer.FirstEmail(parent.Email, user.Email),
                ContactNormalizer.FirstPhone(parent.PrimaryPhone, parent.AlternatePhone, user.PhoneNumber));
    }
}
