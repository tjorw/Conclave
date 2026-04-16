using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface ISessionWatchRepository
{
    Task<EditionId?> FindEditionIdBySessionIdAsync(SessionId sessionId, CancellationToken ct = default);
    Task<bool> ExistsAsync(PersonId personId, SessionId sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<MyWatchedSessionSummaryDto>> ListByPersonAndEditionAsync(PersonId personId, EditionId editionId, CancellationToken ct = default);
    Task AddAndSaveAsync(SessionWatch watch, CancellationToken ct = default);
    Task RemoveByPersonAndSessionAsync(PersonId personId, SessionId sessionId, CancellationToken ct = default);
}
