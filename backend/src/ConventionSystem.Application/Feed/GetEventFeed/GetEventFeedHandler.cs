using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Feed.GetEventFeed;

public sealed class GetEventFeedHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository)
    : IQueryHandler<GetEventFeedQuery, EventFeedDto?>
{
    public async Task<EventFeedDto?> Handle(GetEventFeedQuery query, CancellationToken ct)
    {
        var ev = await eventRepository.GetProjectedByIdAsync(new EventId(query.EventId), ct);
        if (ev is null || ev.Status != "Published") return null;

        var edition = await editionRepository.GetProjectedByIdAsync(new EditionId(ev.EditionId), ct);

        var venueIndex = edition?.Venues.ToDictionary(v => v.Id, v => v.Name)
                         ?? new Dictionary<Guid, string>();
        var categoryName = edition?.Categories.FirstOrDefault(c => c.Id == ev.CategoryId)?.Name;

        var sessions = ev.Sessions
            .Where(s => s.Status == "Active")
            .Select(s => new SessionFeedDto(
                s.Id,
                s.VenueId,
                venueIndex.GetValueOrDefault(s.VenueId, "Okänd lokal"),
                s.Start,
                s.End,
                s.MaxSeats,
                s.StartType))
            .ToList();

        return new EventFeedDto(
            ev.Id,
            ev.EditionId,
            ev.CategoryId,
            categoryName,
            ev.Title,
            ev.Description,
            ev.RegistrationType,
            ev.DropInRules,
            sessions);
    }
}
