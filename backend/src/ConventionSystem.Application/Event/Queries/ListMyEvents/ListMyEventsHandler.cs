using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Event.Queries.ListMyEvents;

public sealed class ListMyEventsHandler(IEventRepository eventRepository, ICurrentUser currentUser)
    : IQueryHandler<ListMyEventsQuery, IReadOnlyList<EventSummaryDto>>
{
    public Task<IReadOnlyList<EventSummaryDto>> Handle(ListMyEventsQuery query, CancellationToken ct)
        => eventRepository.ListByEditionAndOrganiserAsync(
            new EditionId(query.EditionId), currentUser.PersonId, ct);
}
