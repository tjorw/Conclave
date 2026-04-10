using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Abstractions;

public interface IEventRepository
{
    Task AddAndSaveAsync(Domain.Event.Aggregates.Event ev, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithDraftVersionAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithSessionsAsync(EventId id, CancellationToken ct = default);
    Task<IReadOnlyList<EventSummaryDto>> ListByEditionIdAsync(EditionId id, CancellationToken ct = default);
    Task<EventDto?> GetProjectedByIdAsync(EventId id, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
