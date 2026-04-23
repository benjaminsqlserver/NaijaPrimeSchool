using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Users;
using NaijaPrimeSchool.Application.Users.Dtos;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class LookupService(ApplicationDbContext db) : ILookupService
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
}
