using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Family;
using NaijaPrimeSchool.Application.Family.Dtos;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class EnrolmentService(ApplicationDbContext db) : IEnrolmentService
{
    private static IQueryable<EnrolmentDto> Project(IQueryable<Enrolment> q) =>
        q.Select(e => new EnrolmentDto
        {
            Id = e.Id,
            StudentId = e.StudentId,
            StudentName = (e.Student!.FirstName + " " + e.Student!.LastName).Trim(),
            StudentAdmissionNumber = e.Student!.AdmissionNumber,
            SchoolClassId = e.SchoolClassId,
            SchoolClassName = e.SchoolClass!.Name,
            SessionId = e.SchoolClass!.SessionId,
            SessionName = e.SchoolClass!.Session!.Name,
            EnrolledOn = e.EnrolledOn,
            WithdrawnOn = e.WithdrawnOn,
            EnrolmentStatusId = e.EnrolmentStatusId,
            EnrolmentStatusName = e.EnrolmentStatus!.Name,
            Notes = e.Notes,
        });

    public async Task<IReadOnlyList<EnrolmentDto>> ListAsync(EnrolmentListFilter filter, CancellationToken ct = default)
    {
        var q = db.Enrolments.AsQueryable();

        if (filter.StudentId.HasValue) q = q.Where(e => e.StudentId == filter.StudentId.Value);
        if (filter.SchoolClassId.HasValue) q = q.Where(e => e.SchoolClassId == filter.SchoolClassId.Value);
        if (filter.SessionId.HasValue) q = q.Where(e => e.SchoolClass!.SessionId == filter.SessionId.Value);
        if (filter.EnrolmentStatusId.HasValue) q = q.Where(e => e.EnrolmentStatusId == filter.EnrolmentStatusId.Value);

        return await Project(q.OrderByDescending(e => e.EnrolledOn))
            .ToListAsync(ct);
    }

    public Task<EnrolmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Project(db.Enrolments.Where(e => e.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateEnrolmentRequest request, CancellationToken ct = default)
    {
        if (!await db.Students.AnyAsync(s => s.Id == request.StudentId, ct))
            return OperationResult<Guid>.Failure("Student not found.");

        var schoolClass = await db.SchoolClasses
            .FirstOrDefaultAsync(c => c.Id == request.SchoolClassId, ct);
        if (schoolClass is null)
            return OperationResult<Guid>.Failure("Class not found.");

        if (await db.Enrolments.AnyAsync(e =>
                e.StudentId == request.StudentId
                && e.SchoolClassId == request.SchoolClassId, ct))
            return OperationResult<Guid>.Failure("Student is already enrolled in this class.");

        // Within a session a student can be in at most one class at a time.
        var sessionId = schoolClass.SessionId;
        var hasOpenInSession = await db.Enrolments.AnyAsync(e =>
            e.StudentId == request.StudentId
            && e.SchoolClass!.SessionId == sessionId
            && e.WithdrawnOn == null, ct);
        if (hasOpenInSession)
            return OperationResult<Guid>.Failure(
                "Student already has an active enrolment in this session. Withdraw it first.");

        Guid statusId;
        if (request.EnrolmentStatusId.HasValue)
        {
            if (!await db.EnrolmentStatuses.AnyAsync(s => s.Id == request.EnrolmentStatusId.Value, ct))
                return OperationResult<Guid>.Failure("Enrolment status is not valid.");
            statusId = request.EnrolmentStatusId.Value;
        }
        else
        {
            var defaultStatus = await db.EnrolmentStatuses
                .OrderBy(s => s.DisplayOrder)
                .FirstOrDefaultAsync(ct);
            if (defaultStatus is null)
                return OperationResult<Guid>.Failure("Enrolment statuses are not seeded.");
            statusId = defaultStatus.Id;
        }

        var enrolment = new Enrolment
        {
            StudentId = request.StudentId,
            SchoolClassId = request.SchoolClassId,
            EnrolledOn = request.EnrolledOn,
            EnrolmentStatusId = statusId,
            Notes = request.Notes,
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(enrolment.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateEnrolmentRequest request, CancellationToken ct = default)
    {
        var enrolment = await db.Enrolments.FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (enrolment is null) return OperationResult.Failure("Enrolment not found.");

        if (!await db.SchoolClasses.AnyAsync(c => c.Id == request.SchoolClassId, ct))
            return OperationResult.Failure("Class not found.");

        if (!await db.EnrolmentStatuses.AnyAsync(s => s.Id == request.EnrolmentStatusId, ct))
            return OperationResult.Failure("Enrolment status is not valid.");

        if (request.WithdrawnOn.HasValue && request.WithdrawnOn.Value < request.EnrolledOn)
            return OperationResult.Failure("Withdrawal date cannot be earlier than the enrolment date.");

        if (await db.Enrolments.AnyAsync(e =>
                e.StudentId == enrolment.StudentId
                && e.SchoolClassId == request.SchoolClassId
                && e.Id != request.Id, ct))
            return OperationResult.Failure("Student is already enrolled in this class.");

        enrolment.SchoolClassId = request.SchoolClassId;
        enrolment.EnrolledOn = request.EnrolledOn;
        enrolment.WithdrawnOn = request.WithdrawnOn;
        enrolment.EnrolmentStatusId = request.EnrolmentStatusId;
        enrolment.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> WithdrawAsync(Guid id, DateOnly withdrawnOn, string? notes, CancellationToken ct = default)
    {
        var enrolment = await db.Enrolments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrolment is null) return OperationResult.Failure("Enrolment not found.");

        if (withdrawnOn < enrolment.EnrolledOn)
            return OperationResult.Failure("Withdrawal date cannot be earlier than the enrolment date.");

        var withdrawnStatus = await db.EnrolmentStatuses
            .FirstOrDefaultAsync(s => s.Name == "Withdrawn", ct);

        enrolment.WithdrawnOn = withdrawnOn;
        if (withdrawnStatus is not null) enrolment.EnrolmentStatusId = withdrawnStatus.Id;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            enrolment.Notes = string.IsNullOrWhiteSpace(enrolment.Notes)
                ? notes
                : enrolment.Notes + "\n" + notes;
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var enrolment = await db.Enrolments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrolment is null) return OperationResult.Failure("Enrolment not found.");

        db.Enrolments.Remove(enrolment);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }
}
