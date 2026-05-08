using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface ITeamEventRegistrationRepository
{
    Task AddAndSaveAsync(TeamEventRegistration registration, CancellationToken ct = default);
    Task<TeamEventRegistration?> GetByIdAsync(TeamEventRegistrationId id, CancellationToken ct = default);
    Task<bool> HasActiveRegistrationAsync(PersonId captainPersonId, EventId eventId, CancellationToken ct = default);
    Task<IReadOnlyList<TeamRegistrationSummaryDto>> ListByEventIdAsync(EventId eventId, CancellationToken ct = default);
    Task<TeamRegistrationDetailDto?> GetDetailByIdAsync(TeamEventRegistrationId id, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
