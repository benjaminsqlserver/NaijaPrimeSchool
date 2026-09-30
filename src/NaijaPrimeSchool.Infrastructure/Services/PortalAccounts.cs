using Microsoft.AspNetCore.Identity;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Infrastructure.Services;

/// <summary>
/// Keeps a parent's or pupil's portal sign-in (auto-provisioned in Sprint 9)
/// in step with their family record, so deactivating the record also stops
/// the login, exactly as deactivating a staff account does.
/// </summary>
internal static class PortalAccounts
{
    public static async Task<IdentityResult> SetActiveAsync(
        UserManager<ApplicationUser> userManager, Guid? userId, bool isActive, string reason, string modifiedBy)
    {
        if (userId is null) return IdentityResult.Success;
        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || user.IsActive == isActive) return IdentityResult.Success;

        user.IsActive = isActive;
        user.DeactivatedOn = isActive ? null : DateTimeOffset.UtcNow;
        user.DeactivationReason = isActive ? null : reason;
        user.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
        user.ModifiedOn = DateTimeOffset.UtcNow;
        user.ModifiedBy = modifiedBy;

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded) return update;

        // Signs the family member out of any open sessions.
        return await userManager.UpdateSecurityStampAsync(user);
    }
}
