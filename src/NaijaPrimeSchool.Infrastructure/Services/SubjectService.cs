using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Academics;
using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class SubjectService(ApplicationDbContext db) : ISubjectService
{
    public async Task<IReadOnlyList<SubjectDto>> ListAsync(CancellationToken ct = default) =>
        await db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Description = s.Description,
            })
            .ToListAsync(ct);

    public Task<SubjectDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Subjects
            .Where(s => s.Id == id)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Description = s.Description,
            })
            .FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateSubjectRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();

        if (await db.Subjects.AnyAsync(s => s.Name == name, ct))
            return OperationResult<Guid>.Failure($"Subject '{name}' already exists.");

        if (await db.Subjects.AnyAsync(s => s.Code == code, ct))
            return OperationResult<Guid>.Failure($"Subject code '{code}' is already in use.");

        var entity = new Subject
        {
            Name = name,
            Code = code,
            Description = request.Description,
        };
        db.Subjects.Add(entity);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(entity.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateSubjectRequest request, CancellationToken ct = default)
    {
        var entity = await db.Subjects.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (entity is null) return OperationResult.Failure("Subject not found.");

        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();

        if (await db.Subjects.AnyAsync(s => s.Name == name && s.Id != request.Id, ct))
            return OperationResult.Failure($"Subject '{name}' already exists.");

        if (await db.Subjects.AnyAsync(s => s.Code == code && s.Id != request.Id, ct))
            return OperationResult.Failure($"Subject code '{code}' is already in use.");

        entity.Name = name;
        entity.Code = code;
        entity.Description = request.Description;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Subjects
            .Include(s => s.TimetableEntries)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null) return OperationResult.Failure("Subject not found.");

        if (entity.TimetableEntries.Count > 0)
            return OperationResult.Failure("Cannot delete a subject that is on a timetable.");

        db.Subjects.Remove(entity);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
