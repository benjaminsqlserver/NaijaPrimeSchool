using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Users.Dtos;

namespace NaijaPrimeSchool.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(UserListFilter filter, CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<Guid>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateUserRequest request, CancellationToken ct = default);
    Task<OperationResult> SetActiveAsync(Guid userId, bool isActive, string? reason, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid userId, CancellationToken ct = default);
    Task<OperationResult> AssignRolesAsync(Guid userId, IEnumerable<string> roles, CancellationToken ct = default);
    Task<OperationResult> SetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct = default);
}
