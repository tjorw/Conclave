using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Feed.GetEditionFeed;

namespace ConventionSystem.Application.Feed.GetActiveEditionFeed;

public sealed class GetActiveEditionFeedHandler(
    IConventionRepository conventionRepository,
    IEditionRepository editionRepository,
    IEventRepository eventRepository)
    : IQueryHandler<GetActiveEditionFeedQuery, EditionFeedDto?>
{
    public async Task<EditionFeedDto?> Handle(GetActiveEditionFeedQuery query, CancellationToken ct)
    {
        var activeEditionId = await conventionRepository.GetActiveEditionIdAsync(ct);
        if (activeEditionId is null) return null;

        var innerHandler = new GetEditionFeedHandler(editionRepository, eventRepository);
        return await innerHandler.Handle(new GetEditionFeedQuery(activeEditionId.Value.Value), ct);
    }
}
