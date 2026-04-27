using NaijaPrimeSchool.Application.Academics.Dtos;
using NaijaPrimeSchool.Application.Common;

namespace NaijaPrimeSchool.Application.Academics;

public interface ITimetableService
{
    // Periods
    Task<IReadOnlyList<TimetablePeriodDto>> ListPeriodsAsync(CancellationToken ct = default);
    Task<TimetablePeriodDto?> GetPeriodByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<Guid>> CreatePeriodAsync(CreateTimetablePeriodRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdatePeriodAsync(UpdateTimetablePeriodRequest request, CancellationToken ct = default);
    Task<OperationResult> SoftDeletePeriodAsync(Guid id, CancellationToken ct = default);

    // Entries
    Task<IReadOnlyList<TimetableEntryDto>> ListEntriesAsync(TimetableQuery query, CancellationToken ct = default);
    Task<TimetableEntryDto?> GetEntryByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult<Guid>> UpsertEntryAsync(UpsertTimetableEntryRequest request, CancellationToken ct = default);
    Task<OperationResult> SoftDeleteEntryAsync(Guid id, CancellationToken ct = default);
}
