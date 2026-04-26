using ConventionSystem.Application.Reception.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Reception.Abstractions;

public interface IReceptionScheduleRepository
{
    Task<IReadOnlyList<PersonShiftItemDto>> ListShiftsAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default);

    Task<IReadOnlyList<PersonSessionItemDto>> ListOrganiserSessionsAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default);
}
