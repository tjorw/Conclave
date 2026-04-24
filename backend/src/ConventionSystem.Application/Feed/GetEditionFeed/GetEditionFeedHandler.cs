using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Feed.GetEditionFeed;

public sealed class GetEditionFeedHandler(
    IEditionRepository editionRepository,
    IEventRepository eventRepository,
    ISessionRegistrationRepository sessionRegistrationRepository)
    : IQueryHandler<GetEditionFeedQuery, EditionFeedDto?>
{
    public async Task<EditionFeedDto?> Handle(GetEditionFeedQuery query, CancellationToken ct)
    {
        var edition = await editionRepository.GetProjectedByIdAsync(new EditionId(query.EditionId), ct);
        if (edition is null) return null;

        var allEvents = await eventRepository.ListByEditionIdAsync(new EditionId(query.EditionId), ct);

        var activeSessionIds = allEvents
            .SelectMany(e => e.Sessions)
            .Where(s => s.Status == "Active")
            .Select(s => new SessionId(s.Id))
            .Distinct()
            .ToList();

        var bookedSeatsBySession = await sessionRegistrationRepository
            .CountConfirmedBySessionIdsAsync(activeSessionIds, ct);

        var venueIndex = edition.Venues.ToDictionary(v => v.Id, v => v.Name);

        var publishedEvents = allEvents
            .Where(e => e.Status == "Published" && e.Title is not null)
            .Select(e => new EventSummaryFeedDto(
                e.Id,
                e.CategoryId,
                e.CategoryName,
                e.Title!,
                e.Description,
                e.LeadOrganiserName,
                e.SessionCount,
                e.Sessions
                    .Where(s => s.Status == "Active")
                    .Select(s => new SessionSummaryFeedDto(
                        s.Id,
                        venueIndex.GetValueOrDefault(s.VenueId, "Okänd lokal"),
                        s.Start,
                        s.End,
                        s.MaxSeats,
                        bookedSeatsBySession.GetValueOrDefault(new SessionId(s.Id), 0),
                        s.StartType))
                    .OrderBy(s => s.Start)
                    .ToList()))
            .ToList();

        return new EditionFeedDto(
            edition.Id,
            edition.Name,
            edition.Start,
            edition.End,
            edition.OrganiserRegistrationOpen,
            edition.StaffRegistrationOpen,
            edition.VisitorRegistrationOpen,
            edition.Venues.Select(v => new VenueFeedDto(v.Id, v.Name, v.Building, v.Description)).ToList(),
            edition.Categories.Select(c => new CategoryFeedDto(c.Id, c.Name, c.PublicDescription)).ToList(),
            publishedEvents);
    }
}
