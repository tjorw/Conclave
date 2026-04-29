using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Abstractions;

public interface IEventRepository
{
    Task AddAndSaveAsync(Domain.Event.Aggregates.Event ev, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAndApplicationsAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithInvitationsAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAndInvitationsAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByInvitationCodeAsync(string code, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithSessionsAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithCommentsAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithCommentsAndCoOrganisersAsync(EventId id, CancellationToken ct = default);
    Task<IReadOnlyList<EventSummaryDto>> ListByEditionIdAsync(EditionId id, CancellationToken ct = default);
    Task<IReadOnlyList<EventSummaryDto>> ListByEditionAndOrganiserAsync(EditionId editionId, PersonId organiserId, CancellationToken ct = default);
    Task<EventDto?> GetProjectedByIdAsync(EventId id, CancellationToken ct = default);
    Task<IReadOnlyList<EditionOrganiserDto>> ListOrganisersByEditionIdAsync(EditionId editionId, CancellationToken ct = default);
    Task<IReadOnlyList<EditionSessionDto>> ListSessionsByEditionIdAsync(EditionId editionId, CancellationToken ct = default);
    Task DeleteAsync(Domain.Event.Aggregates.Event ev, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
