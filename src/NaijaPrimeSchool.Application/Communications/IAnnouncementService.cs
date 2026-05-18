using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications.Dtos;

namespace NaijaPrimeSchool.Application.Communications;

public interface IAnnouncementService
{
    Task<IReadOnlyList<AnnouncementDto>> ListAsync(AnnouncementFilter filter, CancellationToken ct = default);
    Task<AnnouncementDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<Guid>> CreateAsync(CreateAnnouncementRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateAnnouncementRequest request, CancellationToken ct = default);
    Task<OperationResult> PublishAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> UnpublishAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<AnnouncementDto>> ListForCurrentUserAsync(int take = 20, CancellationToken ct = default);
    Task<OperationResult> MarkAsReadAsync(Guid announcementId, CancellationToken ct = default);
    Task<int> CountUnreadForCurrentUserAsync(CancellationToken ct = default);
}
