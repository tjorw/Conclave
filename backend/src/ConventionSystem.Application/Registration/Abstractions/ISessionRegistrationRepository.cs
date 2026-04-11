using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface ISessionRegistrationRepository
{
    Task<SessionRegistration?> GetByIdAsync(SessionRegistrationId id, CancellationToken ct = default);
    Task<IReadOnlyList<SessionRegistration>> GetAllConfirmedBySessionIdAsync(SessionId sessionId, CancellationToken ct = default);
    Task<bool> HasRegistrationAsync(PersonId personId, SessionRegistrationId sessionId, CancellationToken ct = default);
    Task AddAndSaveAsync(SessionRegistration registration, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
