using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Feed;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Queries.GetFeaturedEvents;

public sealed class GetFeaturedEventsHandler(
    IConventionRepository conventionRepository,
    IEditionRepository editionRepository,
    IEventRepository eventRepository,
    ISessionRegistrationRepository sessionRegistrationRepository)
    : IQueryHandler<GetFeaturedEventsQuery, IReadOnlyList<EventSummaryFeedDto>>
{
    public async Task<IReadOnlyList<EventSummaryFeedDto>> Handle(GetFeaturedEventsQuery query, CancellationToken ct)
    {
        var activeEditionId = await conventionRepository.GetActiveEditionIdAsync(ct);
        if (activeEditionId is null)
            return [];

        var edition = await editionRepository.GetProjectedByIdAsync(activeEditionId.Value, ct);
        if (edition is null)
            return [];

        var allEvents = await eventRepository.ListByEditionIdAsync(activeEditionId.Value, ct);
        var venueIndex = edition.Venues.ToDictionary(v => v.Id, v => v.Name);

        var publishedEvents = allEvents
            .Where(e => e.Status == "Published" && !string.IsNullOrWhiteSpace(e.Title))
            .ToList();

        var featuredEvents = publishedEvents
            .Where(e => e.IsFeatured)
            .OrderBy(e => e.FeaturedSortOrder)
            .ThenBy(e => e.Title)
            .Take(6)
            .ToList();

        var selectedEvents = featuredEvents.Count > 0
            ? featuredEvents
            : publishedEvents
                .OrderByDescending(e => e.Id)
                .Take(3)
                .ToList();

        var activeSessionIds = selectedEvents
            .SelectMany(e => e.Sessions)
            .Where(s => s.Status == "Active")
            .Select(s => new SessionId(s.Id))
            .Distinct()
            .ToList();

        var bookedSeatsBySession = await sessionRegistrationRepository
            .CountConfirmedBySessionIdsAsync(activeSessionIds, ct);

        return selectedEvents
            .Select(e => new EventSummaryFeedDto(
                e.Id,
                e.CategoryId,
                e.CategoryName,
                e.Title!,
                e.Description,
                e.ProgramTags,
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
    }
}
