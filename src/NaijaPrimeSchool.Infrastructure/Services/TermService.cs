using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Academics;
using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class TermService(ApplicationDbContext db) : ITermService
{
    private static IQueryable<TermDto> Project(IQueryable<Term> q) =>
        q.Select(t => new TermDto
        {
            Id = t.Id,
            SessionId = t.SessionId,
            SessionName = t.Session!.Name,
            TermTypeId = t.TermTypeId,
            TermTypeName = t.TermType!.Name,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            IsCurrent = t.IsCurrent,
        });

    public async Task<IReadOnlyList<TermDto>> ListAsync(Guid? sessionId = null, CancellationToken ct = default)
    {
        var q = db.Terms.AsQueryable();
        if (sessionId.HasValue) q = q.Where(t => t.SessionId == sessionId.Value);
        return await Project(q
                .OrderByDescending(t => t.IsCurrent)
                .ThenBy(t => t.StartDate))
            .ToListAsync(ct);
    }

    public Task<TermDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Project(db.Terms.Where(t => t.Id == id)).FirstOrDefaultAsync(ct);

    public Task<TermDto?> GetCurrentAsync(CancellationToken ct = default) =>
        Project(db.Terms.Where(t => t.IsCurrent)).FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateTermRequest request, CancellationToken ct = default)
    {
        if (request.EndDate <= request.StartDate)
            return OperationResult<Guid>.Failure("End date must be after start date.");

        if (!await db.Sessions.AnyAsync(s => s.Id == request.SessionId, ct))
            return OperationResult<Guid>.Failure("Session not found.");

        if (!await db.TermTypes.AnyAsync(t => t.Id == request.TermTypeId, ct))
            return OperationResult<Guid>.Failure("Term type not found.");

        if (await db.Terms.AnyAsync(t => t.SessionId == request.SessionId && t.TermTypeId == request.TermTypeId, ct))
            return OperationResult<Guid>.Failure("That term already exists for this session.");

        var term = new Term
        {
            SessionId = request.SessionId,
            TermTypeId = request.TermTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
        };

        if (request.IsCurrent)
        {
            await db.Terms.Where(t => t.IsCurrent)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCurrent, false), ct);
        }

        db.Terms.Add(term);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(term.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateTermRequest request, CancellationToken ct = default)
    {
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == request.Id, ct);
        if (term is null) return OperationResult.Failure("Term not found.");

        if (request.EndDate <= request.StartDate)
            return OperationResult.Failure("End date must be after start date.");

        if (await db.Terms.AnyAsync(t => t.SessionId == request.SessionId
                                         && t.TermTypeId == request.TermTypeId
                                         && t.Id != request.Id, ct))
            return OperationResult.Failure("That term already exists for this session.");

        term.SessionId = request.SessionId;
        term.TermTypeId = request.TermTypeId;
        term.StartDate = request.StartDate;
        term.EndDate = request.EndDate;

        if (request.IsCurrent && !term.IsCurrent)
        {
            await db.Terms.Where(t => t.IsCurrent && t.Id != term.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCurrent, false), ct);
        }
        term.IsCurrent = request.IsCurrent;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetCurrentAsync(Guid id, CancellationToken ct = default)
    {
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (term is null) return OperationResult.Failure("Term not found.");

        await db.Terms.Where(t => t.IsCurrent && t.Id != id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCurrent, false), ct);

        term.IsCurrent = true;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var term = await db.Terms
            .Include(t => t.TimetableEntries)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (term is null) return OperationResult.Failure("Term not found.");

        if (term.TimetableEntries.Count > 0)
            return OperationResult.Failure("Cannot delete a term that has timetable entries.");

        db.Terms.Remove(term);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
