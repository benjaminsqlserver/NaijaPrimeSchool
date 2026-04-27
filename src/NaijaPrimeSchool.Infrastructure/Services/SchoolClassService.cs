using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Academics;
using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class SchoolClassService(ApplicationDbContext db) : ISchoolClassService
{
    private static IQueryable<SchoolClassDto> Project(IQueryable<SchoolClass> q) =>
        q.Select(c => new SchoolClassDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ClassLevelId = c.ClassLevelId,
            ClassLevelName = c.ClassLevel!.Name,
            SessionId = c.SessionId,
            SessionName = c.Session!.Name,
            ClassTeacherId = c.ClassTeacherId,
            ClassTeacherName = c.ClassTeacher == null
                ? null
                : (c.ClassTeacher.FirstName + " " + c.ClassTeacher.LastName).Trim(),
        });

    public async Task<IReadOnlyList<SchoolClassDto>> ListAsync(Guid? sessionId = null, CancellationToken ct = default)
    {
        var q = db.SchoolClasses.AsQueryable();
        if (sessionId.HasValue) q = q.Where(c => c.SessionId == sessionId.Value);
        return await Project(q
                .OrderBy(c => c.ClassLevel!.DisplayOrder)
                .ThenBy(c => c.Name))
            .ToListAsync(ct);
    }

    public Task<SchoolClassDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Project(db.SchoolClasses.Where(c => c.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateSchoolClassRequest request, CancellationToken ct = default)
    {
        if (!await db.Sessions.AnyAsync(s => s.Id == request.SessionId, ct))
            return OperationResult<Guid>.Failure("Session not found.");

        if (!await db.ClassLevels.AnyAsync(l => l.Id == request.ClassLevelId, ct))
            return OperationResult<Guid>.Failure("Class level not found.");

        if (await db.SchoolClasses.AnyAsync(c => c.SessionId == request.SessionId && c.Name == request.Name, ct))
            return OperationResult<Guid>.Failure($"A class named '{request.Name}' already exists in this session.");

        var entity = new SchoolClass
        {
            Name = request.Name,
            Description = request.Description,
            ClassLevelId = request.ClassLevelId,
            SessionId = request.SessionId,
            ClassTeacherId = request.ClassTeacherId,
        };

        db.SchoolClasses.Add(entity);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(entity.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateSchoolClassRequest request, CancellationToken ct = default)
    {
        var entity = await db.SchoolClasses.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (entity is null) return OperationResult.Failure("Class not found.");

        if (await db.SchoolClasses.AnyAsync(c =>
                c.SessionId == request.SessionId
                && c.Name == request.Name
                && c.Id != request.Id, ct))
            return OperationResult.Failure($"A class named '{request.Name}' already exists in this session.");

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.ClassLevelId = request.ClassLevelId;
        entity.SessionId = request.SessionId;
        entity.ClassTeacherId = request.ClassTeacherId;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.SchoolClasses
            .Include(c => c.TimetableEntries)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return OperationResult.Failure("Class not found.");

        if (entity.TimetableEntries.Count > 0)
            return OperationResult.Failure("Cannot delete a class that has timetable entries.");

        db.SchoolClasses.Remove(entity);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
