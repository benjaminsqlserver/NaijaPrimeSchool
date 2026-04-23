using NaijaPrimeSchool.Application.Users.Dtos;

namespace NaijaPrimeSchool.Application.Users;

public interface ILookupService
{
    Task<IReadOnlyList<LookupDto>> GetGendersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupDto>> GetTitlesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);
}
