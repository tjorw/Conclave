using ConventionSystem.Application.Staff.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Application.Staff.Abstractions;

public interface IShiftRepository
{
    Task AddAndSaveAsync(Shift shift, CancellationToken ct = default);
    Task<Shift?> GetByIdAsync(ShiftId id, CancellationToken ct = default);
    Task<Shift?> GetByIdWithAssignmentsAsync(ShiftId id, CancellationToken ct = default);
    Task<IReadOnlyList<ShiftSummaryDto>> ListByStationIdAsync(StationId id, CancellationToken ct = default);
    Task<ShiftDto?> GetProjectedByIdAsync(ShiftId id, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
