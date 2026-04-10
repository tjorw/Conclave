namespace ConventionSystem.Application.Event.Abstractions;

using ConventionSystem.Domain.Event.Ids;

public interface IEventRepository
{
    Task AddAndSaveAsync(Domain.Event.Aggregates.Event ev, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithDraftVersionAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAsync(EventId id, CancellationToken ct = default);
    Task<Domain.Event.Aggregates.Event?> GetByIdWithSessionsAsync(EventId id, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
