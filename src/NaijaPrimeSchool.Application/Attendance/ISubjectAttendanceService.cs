using NaijaPrimeSchool.Application.Attendance.Dtos;
using NaijaPrimeSchool.Application.Common;

namespace NaijaPrimeSchool.Application.Attendance;

public interface ISubjectAttendanceService
{
    Task<IReadOnlyList<SubjectAttendanceSessionDto>> ListAsync(SubjectSessionListFilter filter, CancellationToken ct = default);
    Task<SubjectAttendanceSessionDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SubjectAttendanceSessionDetailDto?> GetForEntryDateAsync(Guid timetableEntryId, DateOnly date, CancellationToken ct = default);

    Task<OperationResult<Guid>> OpenAsync(OpenSubjectSessionRequest request, CancellationToken ct = default);
    Task<OperationResult> SetEntryAsync(UpsertSubjectEntryRequest request, CancellationToken ct = default);
    Task<OperationResult> BulkSetAsync(BulkSetSubjectAttendanceRequest request, CancellationToken ct = default);
    Task<OperationResult> SubmitAsync(Guid sessionId, CancellationToken ct = default);
    Task<OperationResult> ReopenAsync(Guid sessionId, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid sessionId, CancellationToken ct = default);
}
