using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Event.Queries.ListEvents;

public sealed class ListEventsHandler(IEventRepository eventRepository)
    : IQueryHandler<ListEventsQuery, IReadOnlyList<EventSummaryDto>>
{
    public Task<IReadOnlyList<EventSummaryDto>> Handle(ListEventsQuery query, CancellationToken ct)
        => eventRepository.ListByEditionIdAsync(new EditionId(query.EditionId), ct);
}
