using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Feed.GetEventFeed;

public sealed class GetEventFeedHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    ISessionRegistrationRepository sessionRegistrationRepository)
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

        var activeSessions = ev.Sessions
            .Where(s => s.Status == "Active")
            .ToList();

        var sessionIds = activeSessions
            .Select(s => new SessionId(s.Id))
            .ToList();

        var bookedSeatsBySession = await sessionRegistrationRepository
            .CountConfirmedBySessionIdsAsync(sessionIds, ct);

        var sessions = activeSessions
            .Select(s => new SessionFeedDto(
                s.Id,
                s.VenueId,
                venueIndex.GetValueOrDefault(s.VenueId, "Okänd lokal"),
                s.Start,
                s.End,
                s.MaxSeats,
                bookedSeatsBySession.GetValueOrDefault(new SessionId(s.Id), 0),
                s.StartType))
            .ToList();

        return new EventFeedDto(
            ev.Id,
            ev.EditionId,
            ev.CategoryId,
            categoryName,
            ev.Title,
            ev.Description,
            ev.ProgramTags,
            ev.RegistrationType,
            ev.DropInRules,
            sessions,
            ev.RegistrationMode,
            ev.MinTeamSize,
            ev.MaxTeamSize);
    }
}
