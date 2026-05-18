using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Family;
using NaijaPrimeSchool.Application.Family.Dtos;
using NaijaPrimeSchool.Application.Users.Dtos;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class ParentService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser) : IParentService
{
    public async Task<PagedResult<ParentDto>> ListAsync(ParentListFilter filter, CancellationToken ct = default)
    {
        var query = db.Parents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term) ||
                (p.MiddleName != null && p.MiddleName.ToLower().Contains(term)) ||
                (p.PrimaryPhone != null && p.PrimaryPhone.Contains(term)) ||
                (p.AlternatePhone != null && p.AlternatePhone.Contains(term)) ||
                (p.Email != null && p.Email.ToLower().Contains(term)));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filter.IsActive.Value);
        }

        var total = await query.CountAsync(ct);

        query = filter.OrderBy?.ToLower() switch
        {
            "name desc" => query.OrderByDescending(p => p.FirstName).ThenByDescending(p => p.LastName),
            "createdon desc" => query.OrderByDescending(p => p.CreatedOn),
            _ => query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName),
        };

        var items = await query
            .Skip(filter.Skip)
            .Take(filter.Take)
            .Select(p => new ParentDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                MiddleName = p.MiddleName,
                FullName = p.FirstName + (p.MiddleName == null ? "" : " " + p.MiddleName) + " " + p.LastName,
                TitleId = p.TitleId,
                TitleName = p.Title == null ? null : p.Title.Name,
                GenderId = p.GenderId,
                GenderName = p.Gender == null ? null : p.Gender.Name,
                MaritalStatusId = p.MaritalStatusId,
                MaritalStatusName = p.MaritalStatus == null ? null : p.MaritalStatus.Name,
                PrimaryPhone = p.PrimaryPhone,
                AlternatePhone = p.AlternatePhone,
                Email = p.Email,
                ResidentialAddress = p.ResidentialAddress,
                Occupation = p.Occupation,
                Employer = p.Employer,
                IsActive = p.IsActive,
                StudentCount = p.StudentLinks.Count,
            })
            .ToListAsync(ct);

        return new PagedResult<ParentDto> { Items = items, TotalCount = total };
    }

    public Task<ParentDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Parents.Where(p => p.Id == id)
            .Select(p => new ParentDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                MiddleName = p.MiddleName,
                FullName = p.FirstName + (p.MiddleName == null ? "" : " " + p.MiddleName) + " " + p.LastName,
                TitleId = p.TitleId,
                TitleName = p.Title == null ? null : p.Title.Name,
                GenderId = p.GenderId,
                GenderName = p.Gender == null ? null : p.Gender.Name,
                MaritalStatusId = p.MaritalStatusId,
                MaritalStatusName = p.MaritalStatus == null ? null : p.MaritalStatus.Name,
                PrimaryPhone = p.PrimaryPhone,
                AlternatePhone = p.AlternatePhone,
                Email = p.Email,
                ResidentialAddress = p.ResidentialAddress,
                Occupation = p.Occupation,
                Employer = p.Employer,
                IsActive = p.IsActive,
                StudentCount = p.StudentLinks.Count,
            })
            .FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateParentRequest request, CancellationToken ct = default)
    {
        if (await userManager.FindByNameAsync(request.UserName) is not null)
            return OperationResult<Guid>.Failure($"Username '{request.UserName}' is already taken.");

        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return OperationResult<Guid>.Failure($"Email '{request.Email}' is already in use.");

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PrimaryPhone,
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            TitleId = request.TitleId,
            GenderId = request.GenderId,
            Address = request.ResidentialAddress,
            IsActive = request.IsActive,
            CreatedBy = currentUser.UserName ?? "system",
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return OperationResult<Guid>.Failure(created.Errors.Select(e => e.Description));

        var addRole = await userManager.AddToRoleAsync(user, Roles.Parent);
        if (!addRole.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return OperationResult<Guid>.Failure(addRole.Errors.Select(e => e.Description));
        }

        var parent = new Parent
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            TitleId = request.TitleId,
            GenderId = request.GenderId,
            MaritalStatusId = request.MaritalStatusId,
            PrimaryPhone = request.PrimaryPhone,
            AlternatePhone = request.AlternatePhone,
            Email = request.Email,
            ResidentialAddress = request.ResidentialAddress,
            Occupation = request.Occupation,
            Employer = request.Employer,
            IsActive = request.IsActive,
            UserId = user.Id,
        };
        db.Parents.Add(parent);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(parent.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateParentRequest request, CancellationToken ct = default)
    {
        var parent = await db.Parents.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (parent is null) return OperationResult.Failure("Parent not found.");

        parent.FirstName = request.FirstName.Trim();
        parent.LastName = request.LastName.Trim();
        parent.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim();
        parent.TitleId = request.TitleId;
        parent.GenderId = request.GenderId;
        parent.MaritalStatusId = request.MaritalStatusId;
        parent.PrimaryPhone = request.PrimaryPhone;
        parent.AlternatePhone = request.AlternatePhone;
        parent.Email = request.Email;
        parent.ResidentialAddress = request.ResidentialAddress;
        parent.Occupation = request.Occupation;
        parent.Employer = request.Employer;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var parent = await db.Parents.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (parent is null) return OperationResult.Failure("Parent not found.");

        parent.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var parent = await db.Parents.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (parent is null) return OperationResult.Failure("Parent not found.");

        // Count active links via a fresh database query rather than the
        // Parent.StudentLinks navigation. EF Core relationship fix-up
        // populates that navigation with every StudentParent currently in
        // the change tracker that matches the FK — including soft-deleted
        // rows that an earlier unlink in the same circuit marked
        // IsDeleted = true. The global query filter only affects new
        // SELECTs, not the in-memory graph, so the navigation count would
        // stay > 0 after a successful unlink and block this delete.
        var activeLinks = await db.StudentParents
            .CountAsync(l => l.ParentId == id, ct);

        if (activeLinks > 0)
            return OperationResult.Failure(
                $"Cannot delete this parent because {activeLinks} active student link(s) remain. Unlink them first.");

        db.Parents.Remove(parent);
        await db.SaveChangesAsync(ct);

        // Sprint 9 auto-provisions an ApplicationUser when a parent is
        // created. Mirror that on the way out so a deleted parent cannot
        // sign in to the portal and hit the "we can't find your record"
        // fallback card forever.
        if (parent.UserId is { } userId)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
            {
                await userManager.DeleteAsync(user);
            }
        }

        return OperationResult.Success();
    }

    public Task<IReadOnlyList<StudentParentDto>> GetStudentLinksAsync(Guid parentId, CancellationToken ct = default) =>
        ProjectLinks(db.StudentParents.Where(l => l.ParentId == parentId), ct);

    private static async Task<IReadOnlyList<StudentParentDto>> ProjectLinks(
        IQueryable<StudentParent> q, CancellationToken ct) =>
        await q.OrderBy(l => l.Student!.FirstName)
            .Select(l => new StudentParentDto
            {
                Id = l.Id,
                StudentId = l.StudentId,
                StudentName = (l.Student!.FirstName + " " + l.Student!.LastName).Trim(),
                StudentAdmissionNumber = l.Student!.AdmissionNumber,
                ParentId = l.ParentId,
                ParentName = (l.Parent!.FirstName + " " + l.Parent!.LastName).Trim(),
                ParentPhone = l.Parent!.PrimaryPhone,
                ParentEmail = l.Parent!.Email,
                RelationshipId = l.RelationshipId,
                RelationshipName = l.Relationship!.Name,
                IsPrimaryContact = l.IsPrimaryContact,
                CanPickUp = l.CanPickUp,
                Notes = l.Notes,
            })
            .ToListAsync(ct);
}
