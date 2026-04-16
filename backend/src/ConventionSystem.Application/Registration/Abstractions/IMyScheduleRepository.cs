using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface IMyScheduleRepository
{
    Task<IReadOnlyList<MyScheduleItemDto>> GetMyScheduleAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default);
}
