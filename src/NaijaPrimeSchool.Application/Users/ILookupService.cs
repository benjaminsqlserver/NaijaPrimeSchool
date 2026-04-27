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
}
