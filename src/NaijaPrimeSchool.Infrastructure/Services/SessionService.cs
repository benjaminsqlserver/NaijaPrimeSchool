using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Academics;
using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Domain.Academics;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class SessionService(ApplicationDbContext db) : ISessionService
{
    public async Task<IReadOnlyList<SessionDto>> ListAsync(CancellationToken ct = default) =>
        await db.Sessions
            .OrderByDescending(s => s.IsCurrent)
            .ThenByDescending(s => s.StartDate)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsCurrent = s.IsCurrent,
                TermCount = s.Terms.Count,
                ClassCount = s.Classes.Count,
            })
            .ToListAsync(ct);

    public async Task<SessionDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Sessions
            .Where(s => s.Id == id)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsCurrent = s.IsCurrent,
                TermCount = s.Terms.Count,
                ClassCount = s.Classes.Count,
            })
            .FirstOrDefaultAsync(ct);

    public Task<SessionDto?> GetCurrentAsync(CancellationToken ct = default) =>
        db.Sessions
            .Where(s => s.IsCurrent)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsCurrent = s.IsCurrent,
                TermCount = s.Terms.Count,
                ClassCount = s.Classes.Count,
            })
            .FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateSessionRequest request, CancellationToken ct = default)
    {
        if (request.EndDate <= request.StartDate)
            return OperationResult<Guid>.Failure("End date must be after start date.");

        if (await db.Sessions.AnyAsync(s => s.Name == request.Name, ct))
            return OperationResult<Guid>.Failure($"A session named '{request.Name}' already exists.");

        var session = new Session
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
        };

        if (request.IsCurrent)
        {
            await db.Sessions.Where(s => s.IsCurrent)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsCurrent, false), ct);
        }

        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(session.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateSessionRequest request, CancellationToken ct = default)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (session is null) return OperationResult.Failure("Session not found.");

        if (request.EndDate <= request.StartDate)
            return OperationResult.Failure("End date must be after start date.");

        if (await db.Sessions.AnyAsync(s => s.Name == request.Name && s.Id != request.Id, ct))
            return OperationResult.Failure($"A session named '{request.Name}' already exists.");

        session.Name = request.Name;
        session.StartDate = request.StartDate;
        session.EndDate = request.EndDate;

        if (request.IsCurrent && !session.IsCurrent)
        {
            await db.Sessions.Where(s => s.IsCurrent && s.Id != session.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsCurrent, false), ct);
        }
        session.IsCurrent = request.IsCurrent;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetCurrentAsync(Guid id, CancellationToken ct = default)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return OperationResult.Failure("Session not found.");

        await db.Sessions.Where(s => s.IsCurrent && s.Id != id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsCurrent, false), ct);

        session.IsCurrent = true;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var session = await db.Sessions
            .Include(s => s.Terms)
            .Include(s => s.Classes)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return OperationResult.Failure("Session not found.");

        if (session.Terms.Count > 0 || session.Classes.Count > 0)
            return OperationResult.Failure("Cannot delete a session that still has terms or classes attached.");

        db.Sessions.Remove(session);  // SaveChanges turns this into a soft delete
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
