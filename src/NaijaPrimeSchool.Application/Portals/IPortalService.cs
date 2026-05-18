using NaijaPrimeSchool.Application.Portals.Dtos;

namespace NaijaPrimeSchool.Application.Portals;

public interface IPortalService
{
    // Resolution: from the currently-signed-in user back to a Parent or
    // Student row. Returns null when the user does not have a matching row
    // linked via UserId.
    Task<Guid?> ResolveParentIdForCurrentUserAsync(CancellationToken ct = default);
    Task<Guid?> ResolveStudentIdForCurrentUserAsync(CancellationToken ct = default);

    Task<ParentDashboardDto?> GetParentDashboardAsync(Guid parentId, CancellationToken ct = default);
    Task<StudentDashboardDto?> GetStudentDashboardAsync(Guid studentId, CancellationToken ct = default);

    // Access guard: returns true when the currently-signed-in user is a
    // parent linked to the given student. SuperAdmin / HeadTeacher always
    // returns true.
    Task<bool> CurrentUserCanViewStudentAsync(Guid studentId, CancellationToken ct = default);
}
