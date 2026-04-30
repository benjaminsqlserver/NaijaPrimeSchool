using NaijaPrimeSchool.Application.Users.Dtos;

namespace NaijaPrimeSchool.Application.Users;

public interface ILookupService
{
    Task<IReadOnlyList<LookupDto>> GetGendersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetTitlesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LookupDto>> GetTermTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetClassLevelsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetWeekDaysAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetTeachersAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LookupDto>> GetRelationshipsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetEnrolmentStatusesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetBloodGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetMaritalStatusesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetClassesForSessionAsync(Guid? sessionId = null, CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetStudentsAsync(string? searchTerm = null, CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetParentsAsync(string? searchTerm = null, CancellationToken ct = default);
}
