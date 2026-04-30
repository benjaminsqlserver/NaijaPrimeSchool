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

    public async Task<IReadOnlyList<LookupDto>> GetRelationshipsAsync(CancellationToken ct = default) =>
        await db.Relationships
            .OrderBy(r => r.DisplayOrder)
            .Select(r => new LookupDto { Id = r.Id, Name = r.Name })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetEnrolmentStatusesAsync(CancellationToken ct = default) =>
        await db.EnrolmentStatuses
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new LookupDto { Id = s.Id, Name = s.Name })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetBloodGroupsAsync(CancellationToken ct = default) =>
        await db.BloodGroups
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new LookupDto { Id = g.Id, Name = g.Name })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetMaritalStatusesAsync(CancellationToken ct = default) =>
        await db.MaritalStatuses
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new LookupDto { Id = m.Id, Name = m.Name })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupDto>> GetClassesForSessionAsync(Guid? sessionId = null, CancellationToken ct = default)
    {
        var query = db.SchoolClasses.AsQueryable();
        if (sessionId.HasValue)
            query = query.Where(c => c.SessionId == sessionId.Value);

        return await query
            .OrderBy(c => c.ClassLevel!.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new LookupDto
            {
                Id = c.Id,
                Name = c.Name + " — " + c.Session!.Name,
                Code = c.Session!.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LookupDto>> GetStudentsAsync(string? searchTerm = null, CancellationToken ct = default)
    {
        var query = db.Students.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                s.AdmissionNumber.ToLower().Contains(term));
        }

        return await query
            .OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
            .Take(50)
            .Select(s => new LookupDto
            {
                Id = s.Id,
                Name = (s.FirstName + " " + s.LastName).Trim(),
                Code = s.AdmissionNumber,
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LookupDto>> GetParentsAsync(string? searchTerm = null, CancellationToken ct = default)
    {
        var query = db.Parents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term) ||
                (p.PrimaryPhone != null && p.PrimaryPhone.Contains(term)) ||
                (p.Email != null && p.Email.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
            .Take(50)
            .Select(p => new LookupDto
            {
                Id = p.Id,
                Name = (p.FirstName + " " + p.LastName).Trim(),
                Code = p.PrimaryPhone,
            })
            .ToListAsync(ct);
    }
}
