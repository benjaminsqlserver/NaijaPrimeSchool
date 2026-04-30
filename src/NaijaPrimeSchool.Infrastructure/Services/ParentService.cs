using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Family;
using NaijaPrimeSchool.Application.Family.Dtos;
using NaijaPrimeSchool.Application.Users.Dtos;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class ParentService(ApplicationDbContext db) : IParentService
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
        var parent = await db.Parents
            .Include(p => p.StudentLinks)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (parent is null) return OperationResult.Failure("Parent not found.");

        if (parent.StudentLinks.Count > 0)
            return OperationResult.Failure(
                "Cannot delete a parent who is still linked to a student. Unlink them first.");

        db.Parents.Remove(parent);
        await db.SaveChangesAsync(ct);
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
