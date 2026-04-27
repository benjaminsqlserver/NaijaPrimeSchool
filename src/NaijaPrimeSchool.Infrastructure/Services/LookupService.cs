using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Users;
using NaijaPrimeSchool.Application.Users.Dtos;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class LookupService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : ILookupService
{
    public async Task<IReadOnlyList<LookupDto>> GetGendersAsync(CancellationToken ct = default) =>
        await db.Genders
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new LookupDto { Id = g.Id, Name = g.Name, Code = g.Code })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetTitlesAsync(CancellationToken ct = default) =>
        await db.Titles
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new LookupDto { Id = t.Id, Name = t.Name })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default) =>
        await db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetTermTypesAsync(CancellationToken ct = default) =>
        await db.TermTypes
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new LookupDto { Id = t.Id, Name = t.Name })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetClassLevelsAsync(CancellationToken ct = default) =>
        await db.ClassLevels
            .OrderBy(l => l.DisplayOrder)
            .Select(l => new LookupDto { Id = l.Id, Name = l.Name })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetWeekDaysAsync(CancellationToken ct = default) =>
        await db.WeekDays
            .OrderBy(d => d.DisplayOrder)
            .Select(d => new LookupDto { Id = d.Id, Name = d.Name, Code = d.ShortName })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetTeachersAsync(CancellationToken ct = default)
    {
        var teachers = await userManager.GetUsersInRoleAsync(Roles.Teacher);
        return teachers
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new LookupDto
            {
                Id = u.Id,
                Name = (u.FirstName + " " + u.LastName).Trim(),
                Code = u.UserName,
            })
            .ToList();
    }
}
