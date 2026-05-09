using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface ISessionRegistrationRepository
{
    Task<SessionRegistration?> GetByIdAsync(SessionRegistrationId id, CancellationToken ct = default);
    Task<IReadOnlyList<SessionRegistration>> GetAllConfirmedBySessionIdAsync(SessionId sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<SessionRegistration>> GetAllConfirmedByTicketIdAsync(TicketId ticketId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<SessionId, int>> CountConfirmedBySessionIdsAsync(IReadOnlyCollection<SessionId> sessionIds, CancellationToken ct = default);
    Task<bool> HasRegistrationAsync(PersonId personId, SessionId sessionId, CancellationToken ct = default);
    Task<int> CountConfirmedBySessionIdAsync(SessionId sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<SessionRegistration>> GetPendingBySessionAsync(SessionId sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<MySessionRegistrationSummaryDto>> ListByPersonAndEditionAsync(PersonId personId, EditionId editionId, CancellationToken ct = default);
    Task AddAndSaveAsync(SessionRegistration registration, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
    Task SaveAllAsync(IReadOnlyList<SessionRegistration> registrations, CancellationToken ct = default);
}
