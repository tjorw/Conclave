using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Queries.GetEvent;

public sealed class GetEventHandler(IEventRepository eventRepository)
    : IQueryHandler<GetEventQuery, EventDto?>
{
    public Task<EventDto?> Handle(GetEventQuery query, CancellationToken ct)
        => eventRepository.GetProjectedByIdAsync(new EventId(query.EventId), ct);
}
