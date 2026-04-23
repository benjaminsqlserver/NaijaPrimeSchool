using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Users;
using NaijaPrimeSchool.Application.Users.Dtos;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class UserService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ICurrentUser currentUser)
    : IUserService
{
    public async Task<PagedResult<UserDto>> ListAsync(UserListFilter filter, CancellationToken ct = default)
    {
        var query = db.Users
            .Include(u => u.Gender)
            .Include(u => u.Title)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var normalized = filter.Role.Trim().ToUpperInvariant();
            var roleId = await db.Roles
                .Where(r => r.NormalizedName == normalized)
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync(ct);

            if (roleId.HasValue)
            {
                query = query.Where(u =>
                    db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId.Value));
            }
            else
            {
                query = query.Where(_ => false);
            }
        }

        var total = await query.CountAsync(ct);

        query = filter.OrderBy?.ToLower() switch
        {
            "name asc" => query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            "name desc" => query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
            "email asc" => query.OrderBy(u => u.Email),
            "email desc" => query.OrderByDescending(u => u.Email),
            "createdon desc" => query.OrderByDescending(u => u.CreatedOn),
            _ => query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
        };

        var users = await query
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();

        var roleLookup = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, r.Name }
        ).ToListAsync(ct);

        var items = users.Select(u => MapToDto(u, roleLookup
            .Where(x => x.UserId == u.Id)
            .Select(x => x.Name!)
            .ToList())).ToList();

        return new PagedResult<UserDto> { Items = items, TotalCount = total };
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.Gender)
            .Include(u => u.Title)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        return MapToDto(user, roles.ToList());
    }

    public async Task<OperationResult<Guid>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (await userManager.FindByNameAsync(request.UserName) is not null)
            return OperationResult<Guid>.Failure($"Username '{request.UserName}' is already taken.");

        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return OperationResult<Guid>.Failure($"Email '{request.Email}' is already in use.");

        foreach (var role in request.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                return OperationResult<Guid>.Failure($"Role '{role}' does not exist.");
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            TitleId = request.TitleId,
            GenderId = request.GenderId,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address,
            IsActive = request.IsActive,
            CreatedBy = currentUser.UserName ?? "system",
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return OperationResult<Guid>.Failure(created.Errors.Select(e => e.Description));

        if (request.Roles.Count > 0)
        {
            var addRoles = await userManager.AddToRolesAsync(user, request.Roles);
            if (!addRoles.Succeeded)
                return OperationResult<Guid>.Failure(addRoles.Errors.Select(e => e.Description));
        }

        return OperationResult<Guid>.Success(user.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(request.Id.ToString());
        if (user is null) return OperationResult.Failure("User not found.");

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing is not null && existing.Id != user.Id)
                return OperationResult.Failure($"Email '{request.Email}' is already in use.");

            var setEmail = await userManager.SetEmailAsync(user, request.Email);
            if (!setEmail.Succeeded)
                return OperationResult.Failure(setEmail.Errors.Select(e => e.Description));
        }

        if (!string.Equals(user.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal))
        {
            var setPhone = await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!setPhone.Succeeded)
                return OperationResult.Failure(setPhone.Errors.Select(e => e.Description));
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.MiddleName = request.MiddleName;
        user.TitleId = request.TitleId;
        user.GenderId = request.GenderId;
        user.DateOfBirth = request.DateOfBirth;
        user.Address = request.Address;
        user.ModifiedOn = DateTimeOffset.UtcNow;
        user.ModifiedBy = currentUser.UserName ?? "system";

        var update = await userManager.UpdateAsync(user);
        return update.Succeeded
            ? OperationResult.Success()
            : OperationResult.Failure(update.Errors.Select(e => e.Description));
    }

    public async Task<OperationResult> SetActiveAsync(Guid userId, bool isActive, string? reason, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return OperationResult.Failure("User not found.");

        user.IsActive = isActive;
        user.DeactivatedOn = isActive ? null : DateTimeOffset.UtcNow;
        user.DeactivationReason = isActive ? null : reason;
        user.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
        user.ModifiedOn = DateTimeOffset.UtcNow;
        user.ModifiedBy = currentUser.UserName ?? "system";

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return OperationResult.Failure(update.Errors.Select(e => e.Description));

        await userManager.UpdateSecurityStampAsync(user);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return OperationResult.Failure("User not found.");

        user.IsDeleted = true;
        user.DeletedOn = DateTimeOffset.UtcNow;
        user.DeletedBy = currentUser.UserName ?? "system";
        user.IsActive = false;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return OperationResult.Failure(update.Errors.Select(e => e.Description));

        await userManager.UpdateSecurityStampAsync(user);
        return OperationResult.Success();
    }

    public async Task<OperationResult> AssignRolesAsync(Guid userId, IEnumerable<string> roles, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return OperationResult.Failure("User not found.");

        var target = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var r in target)
        {
            if (!await roleManager.RoleExistsAsync(r))
                return OperationResult.Failure($"Role '{r}' does not exist.");
        }

        var current = await userManager.GetRolesAsync(user);
        var toRemove = current.Except(target, StringComparer.OrdinalIgnoreCase).ToList();
        var toAdd = target.Except(current, StringComparer.OrdinalIgnoreCase).ToList();

        if (toRemove.Count > 0)
        {
            var remove = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!remove.Succeeded)
                return OperationResult.Failure(remove.Errors.Select(e => e.Description));
        }

        if (toAdd.Count > 0)
        {
            var add = await userManager.AddToRolesAsync(user, toAdd);
            if (!add.Succeeded)
                return OperationResult.Failure(add.Errors.Select(e => e.Description));
        }

        await userManager.UpdateSecurityStampAsync(user);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return OperationResult.Failure("User not found.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, newPassword);
        return reset.Succeeded
            ? OperationResult.Success()
            : OperationResult.Failure(reset.Errors.Select(e => e.Description));
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return [];
        var roles = await userManager.GetRolesAsync(user);
        return roles.ToArray();
    }

    private static UserDto MapToDto(ApplicationUser u, List<string> roles) => new()
    {
        Id = u.Id,
        UserName = u.UserName ?? string.Empty,
        Email = u.Email ?? string.Empty,
        PhoneNumber = u.PhoneNumber,
        FirstName = u.FirstName,
        LastName = u.LastName,
        MiddleName = u.MiddleName,
        FullName = u.FullName,
        TitleId = u.TitleId,
        TitleName = u.Title?.Name,
        GenderId = u.GenderId,
        GenderName = u.Gender?.Name,
        DateOfBirth = u.DateOfBirth,
        Address = u.Address,
        IsActive = u.IsActive,
        EmailConfirmed = u.EmailConfirmed,
        CreatedOn = u.CreatedOn,
        Roles = roles,
    };
}
