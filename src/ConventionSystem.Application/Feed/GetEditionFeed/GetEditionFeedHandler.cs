using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Feed.GetEditionFeed;

public sealed class GetEditionFeedHandler(
    IEditionRepository editionRepository,
    IEventRepository eventRepository)
    : IQueryHandler<GetEditionFeedQuery, EditionFeedDto?>
{
    public async Task<EditionFeedDto?> Handle(GetEditionFeedQuery query, CancellationToken ct)
    {
        var edition = await editionRepository.GetProjectedByIdAsync(new EditionId(query.EditionId), ct);
        if (edition is null) return null;

        var allEvents = await eventRepository.ListByEditionIdAsync(new EditionId(query.EditionId), ct);
        var publishedEvents = allEvents
            .Where(e => e.Status == "Published" && e.Title is not null)
            .Select(e => new EventSummaryFeedDto(e.Id, e.CategoryId, e.Title!, e.SessionCount))
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
            edition.Categories.Select(c => new CategoryFeedDto(c.Id, c.Name, c.Description)).ToList(),
            publishedEvents);
    }
}
