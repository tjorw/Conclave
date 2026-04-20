using ConventionSystem.Application.Feed.GetActiveEditionFeed;
using ConventionSystem.Application.Feed.GetEditionFeed;
using ConventionSystem.Application.Feed.GetEventFeed;
using ConventionSystem.Application.Common;

namespace ConventionSystem.Api.Endpoints;

public static class FeedEndpoints
{
    public static void MapFeedEndpoints(this RouteGroups groups)
    {
        var feed = groups.Anonymous.MapGroup("/feed/{conventionId:guid}");

        feed.MapGet("/editions/{editionId:guid}", async (
            Guid editionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEditionFeedQuery(editionId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        feed.MapGet("/events/{eventId:guid}", async (
            Guid eventId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEventFeedQuery(eventId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        feed.MapGet("/active-edition", async (
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetActiveEditionFeedQuery(), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

    }
}
