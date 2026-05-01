using NaijaPrimeSchool.Application.Attendance.Dtos;
using NaijaPrimeSchool.Application.Common;

namespace NaijaPrimeSchool.Application.Attendance;

public interface IDailyAttendanceService
{
    Task<IReadOnlyList<DailyAttendanceRegisterDto>> ListAsync(DailyRegisterListFilter filter, CancellationToken ct = default);
    Task<DailyAttendanceRegisterDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DailyAttendanceRegisterDetailDto?> GetForClassDateAsync(Guid schoolClassId, DateOnly date, CancellationToken ct = default);

    Task<OperationResult<Guid>> OpenAsync(OpenDailyRegisterRequest request, CancellationToken ct = default);
    Task<OperationResult> SetEntryAsync(UpsertDailyEntryRequest request, CancellationToken ct = default);
    Task<OperationResult> BulkSetAsync(BulkSetDailyAttendanceRequest request, CancellationToken ct = default);
    Task<OperationResult> SubmitAsync(Guid registerId, CancellationToken ct = default);
    Task<OperationResult> ReopenAsync(Guid registerId, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteAsync(Guid registerId, CancellationToken ct = default);

    Task<StudentAttendanceSummaryDto> GetStudentSummaryAsync(StudentAttendanceSummaryFilter filter, CancellationToken ct = default);
    Task<ClassAttendanceSummaryDto> GetClassSummaryAsync(Guid schoolClassId, Guid? termId, CancellationToken ct = default);
}
