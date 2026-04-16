using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface IMyScheduleRepository
{
    Task<IReadOnlyList<MyOrganiserSessionSummaryDto>> ListMyOrganiserSessionsAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default);

    Task<IReadOnlyList<MyAssignedShiftSummaryDto>> ListMyAssignedShiftsAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default);
}
