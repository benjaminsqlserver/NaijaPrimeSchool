using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Family;
using NaijaPrimeSchool.Application.Family.Dtos;
using NaijaPrimeSchool.Application.Users.Dtos;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class StudentService(ApplicationDbContext db) : IStudentService
{
    public async Task<PagedResult<StudentDto>> ListAsync(StudentListFilter filter, CancellationToken ct = default)
    {
        var query = db.Students.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                (s.MiddleName != null && s.MiddleName.ToLower().Contains(term)) ||
                s.AdmissionNumber.ToLower().Contains(term));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == filter.IsActive.Value);
        }

        if (filter.ClassId.HasValue)
        {
            var classId = filter.ClassId.Value;
            query = query.Where(s =>
                s.Enrolments.Any(e => e.SchoolClassId == classId && e.WithdrawnOn == null));
        }

        if (filter.SessionId.HasValue)
        {
            var sessionId = filter.SessionId.Value;
            query = query.Where(s =>
                s.Enrolments.Any(e => e.SchoolClass!.SessionId == sessionId));
        }

        var total = await query.CountAsync(ct);

        query = filter.OrderBy?.ToLower() switch
        {
            "name desc" => query.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName),
            "admission asc" => query.OrderBy(s => s.AdmissionNumber),
            "admission desc" => query.OrderByDescending(s => s.AdmissionNumber),
            "createdon desc" => query.OrderByDescending(s => s.CreatedOn),
            _ => query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName),
        };

        var items = await query
            .Skip(filter.Skip)
            .Take(filter.Take)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                AdmissionNumber = s.AdmissionNumber,
                AdmissionDate = s.AdmissionDate,
                FirstName = s.FirstName,
                LastName = s.LastName,
                MiddleName = s.MiddleName,
                FullName = s.FirstName + (s.MiddleName == null ? "" : " " + s.MiddleName) + " " + s.LastName,
                DateOfBirth = s.DateOfBirth,
                GenderId = s.GenderId,
                GenderName = s.Gender == null ? null : s.Gender.Name,
                BloodGroupId = s.BloodGroupId,
                BloodGroupName = s.BloodGroup == null ? null : s.BloodGroup.Name,
                StateOfOrigin = s.StateOfOrigin,
                ResidentialAddress = s.ResidentialAddress,
                PhotoUrl = s.PhotoUrl,
                Allergies = s.Allergies,
                MedicalNotes = s.MedicalNotes,
                IsActive = s.IsActive,
                CurrentClassId = s.Enrolments
                    .Where(e => e.WithdrawnOn == null)
                    .OrderByDescending(e => e.EnrolledOn)
                    .Select(e => (Guid?)e.SchoolClassId)
                    .FirstOrDefault(),
                CurrentClassName = s.Enrolments
                    .Where(e => e.WithdrawnOn == null)
                    .OrderByDescending(e => e.EnrolledOn)
                    .Select(e => e.SchoolClass!.Name)
                    .FirstOrDefault(),
                CurrentSessionId = s.Enrolments
                    .Where(e => e.WithdrawnOn == null)
                    .OrderByDescending(e => e.EnrolledOn)
                    .Select(e => (Guid?)e.SchoolClass!.SessionId)
                    .FirstOrDefault(),
                CurrentSessionName = s.Enrolments
                    .Where(e => e.WithdrawnOn == null)
                    .OrderByDescending(e => e.EnrolledOn)
                    .Select(e => e.SchoolClass!.Session!.Name)
                    .FirstOrDefault(),
                ParentCount = s.ParentLinks.Count,
                PrimaryContactName = s.ParentLinks
                    .Where(l => l.IsPrimaryContact)
                    .Select(l => l.Parent!.FirstName + " " + l.Parent!.LastName)
                    .FirstOrDefault(),
                PrimaryContactPhone = s.ParentLinks
                    .Where(l => l.IsPrimaryContact)
                    .Select(l => l.Parent!.PrimaryPhone)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        return new PagedResult<StudentDto> { Items = items, TotalCount = total };
    }

    public Task<StudentDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Students.Where(s => s.Id == id)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                AdmissionNumber = s.AdmissionNumber,
                AdmissionDate = s.AdmissionDate,
                FirstName = s.FirstName,
                LastName = s.LastName,
                MiddleName = s.MiddleName,
                FullName = s.FirstName + (s.MiddleName == null ? "" : " " + s.MiddleName) + " " + s.LastName,
                DateOfBirth = s.DateOfBirth,
                GenderId = s.GenderId,
                GenderName = s.Gender == null ? null : s.Gender.Name,
                BloodGroupId = s.BloodGroupId,
                BloodGroupName = s.BloodGroup == null ? null : s.BloodGroup.Name,
                StateOfOrigin = s.StateOfOrigin,
                ResidentialAddress = s.ResidentialAddress,
                PhotoUrl = s.PhotoUrl,
                Allergies = s.Allergies,
                MedicalNotes = s.MedicalNotes,
                IsActive = s.IsActive,
                CurrentClassId = s.Enrolments
                    .Where(e => e.WithdrawnOn == null)
                    .OrderByDescending(e => e.EnrolledOn)
                    .Select(e => (Guid?)e.SchoolClassId)
                    .FirstOrDefault(),
                CurrentClassName = s.Enrolments
                    .Where(e => e.WithdrawnOn == null)
                    .OrderByDescending(e => e.EnrolledOn)
                    .Select(e => e.SchoolClass!.Name)
                    .FirstOrDefault(),
                CurrentSessionId = s.Enrolments
                    .Where(e => e.WithdrawnOn == null)
                    .OrderByDescending(e => e.EnrolledOn)
                    .Select(e => (Guid?)e.SchoolClass!.SessionId)
                    .FirstOrDefault(),
                CurrentSessionName = s.Enrolments
                    .Where(e => e.WithdrawnOn == null)
                    .OrderByDescending(e => e.EnrolledOn)
                    .Select(e => e.SchoolClass!.Session!.Name)
                    .FirstOrDefault(),
                ParentCount = s.ParentLinks.Count,
            })
            .FirstOrDefaultAsync(ct);

    public async Task<OperationResult<Guid>> CreateAsync(CreateStudentRequest request, CancellationToken ct = default)
    {
        if (request.DateOfBirth >= request.AdmissionDate)
            return OperationResult<Guid>.Failure("Date of birth must be earlier than the admission date.");

        if (await db.Students.AnyAsync(s => s.AdmissionNumber == request.AdmissionNumber, ct))
            return OperationResult<Guid>.Failure(
                $"A student with admission number '{request.AdmissionNumber}' already exists.");

        if (request.InitialClassId.HasValue
            && !await db.SchoolClasses.AnyAsync(c => c.Id == request.InitialClassId.Value, ct))
            return OperationResult<Guid>.Failure("Selected class does not exist.");

        var student = new Student
        {
            AdmissionNumber = request.AdmissionNumber.Trim(),
            AdmissionDate = request.AdmissionDate,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            DateOfBirth = request.DateOfBirth,
            GenderId = request.GenderId,
            BloodGroupId = request.BloodGroupId,
            StateOfOrigin = request.StateOfOrigin,
            ResidentialAddress = request.ResidentialAddress,
            PhotoUrl = request.PhotoUrl,
            Allergies = request.Allergies,
            MedicalNotes = request.MedicalNotes,
            IsActive = request.IsActive,
        };

        db.Students.Add(student);

        if (request.InitialClassId.HasValue)
        {
            var defaultStatus = await db.EnrolmentStatuses
                .OrderBy(s => s.DisplayOrder)
                .FirstOrDefaultAsync(ct);

            if (defaultStatus is null)
                return OperationResult<Guid>.Failure("Enrolment statuses are not seeded.");

            db.Enrolments.Add(new Enrolment
            {
                Student = student,
                SchoolClassId = request.InitialClassId.Value,
                EnrolledOn = request.AdmissionDate,
                EnrolmentStatusId = defaultStatus.Id,
            });
        }

        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(student.Id);
    }

    public async Task<OperationResult> UpdateAsync(UpdateStudentRequest request, CancellationToken ct = default)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == request.Id, ct);
        if (student is null) return OperationResult.Failure("Student not found.");

        if (request.DateOfBirth >= request.AdmissionDate)
            return OperationResult.Failure("Date of birth must be earlier than the admission date.");

        if (await db.Students.AnyAsync(s => s.AdmissionNumber == request.AdmissionNumber && s.Id != request.Id, ct))
            return OperationResult.Failure(
                $"A student with admission number '{request.AdmissionNumber}' already exists.");

        student.AdmissionNumber = request.AdmissionNumber.Trim();
        student.AdmissionDate = request.AdmissionDate;
        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();
        student.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim();
        student.DateOfBirth = request.DateOfBirth;
        student.GenderId = request.GenderId;
        student.BloodGroupId = request.BloodGroupId;
        student.StateOfOrigin = request.StateOfOrigin;
        student.ResidentialAddress = request.ResidentialAddress;
        student.PhotoUrl = request.PhotoUrl;
        student.Allergies = request.Allergies;
        student.MedicalNotes = request.MedicalNotes;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null) return OperationResult.Failure("Student not found.");

        student.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var student = await db.Students
            .Include(s => s.Enrolments)
            .Include(s => s.ParentLinks)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (student is null) return OperationResult.Failure("Student not found.");

        if (student.Enrolments.Any(e => e.WithdrawnOn == null))
            return OperationResult.Failure(
                "Cannot delete a student with an active enrolment. Withdraw the enrolment first.");

        db.Students.Remove(student);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public Task<IReadOnlyList<StudentParentDto>> GetParentLinksAsync(Guid studentId, CancellationToken ct = default) =>
        ProjectLinks(db.StudentParents.Where(l => l.StudentId == studentId), ct);

    public async Task<OperationResult<Guid>> LinkParentAsync(LinkStudentParentRequest request, CancellationToken ct = default)
    {
        if (!await db.Students.AnyAsync(s => s.Id == request.StudentId, ct))
            return OperationResult<Guid>.Failure("Student not found.");

        if (!await db.Parents.AnyAsync(p => p.Id == request.ParentId, ct))
            return OperationResult<Guid>.Failure("Parent not found.");

        if (!await db.Relationships.AnyAsync(r => r.Id == request.RelationshipId, ct))
            return OperationResult<Guid>.Failure("Relationship is not valid.");

        if (await db.StudentParents.AnyAsync(l =>
                l.StudentId == request.StudentId && l.ParentId == request.ParentId, ct))
            return OperationResult<Guid>.Failure("This parent is already linked to the student.");

        if (request.IsPrimaryContact)
        {
            await db.StudentParents
                .Where(l => l.StudentId == request.StudentId && l.IsPrimaryContact)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsPrimaryContact, false), ct);
        }

        var link = new StudentParent
        {
            StudentId = request.StudentId,
            ParentId = request.ParentId,
            RelationshipId = request.RelationshipId,
            IsPrimaryContact = request.IsPrimaryContact,
            CanPickUp = request.CanPickUp,
            Notes = request.Notes,
        };
        db.StudentParents.Add(link);
        await db.SaveChangesAsync(ct);
        return OperationResult<Guid>.Success(link.Id);
    }

    public async Task<OperationResult> UpdateParentLinkAsync(UpdateStudentParentLinkRequest request, CancellationToken ct = default)
    {
        var link = await db.StudentParents.FirstOrDefaultAsync(l => l.Id == request.Id, ct);
        if (link is null) return OperationResult.Failure("Link not found.");

        if (!await db.Relationships.AnyAsync(r => r.Id == request.RelationshipId, ct))
            return OperationResult.Failure("Relationship is not valid.");

        if (request.IsPrimaryContact && !link.IsPrimaryContact)
        {
            await db.StudentParents
                .Where(l => l.StudentId == link.StudentId && l.IsPrimaryContact && l.Id != link.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsPrimaryContact, false), ct);
        }

        link.RelationshipId = request.RelationshipId;
        link.IsPrimaryContact = request.IsPrimaryContact;
        link.CanPickUp = request.CanPickUp;
        link.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UnlinkParentAsync(Guid linkId, CancellationToken ct = default)
    {
        var link = await db.StudentParents.FirstOrDefaultAsync(l => l.Id == linkId, ct);
        if (link is null) return OperationResult.Failure("Link not found.");

        db.StudentParents.Remove(link);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    private static async Task<IReadOnlyList<StudentParentDto>> ProjectLinks(
        IQueryable<StudentParent> q, CancellationToken ct) =>
        await q.OrderByDescending(l => l.IsPrimaryContact)
            .ThenBy(l => l.Parent!.FirstName)
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
