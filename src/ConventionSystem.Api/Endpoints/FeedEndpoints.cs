using ConventionSystem.Application.Feed.GetEditionFeed;
using ConventionSystem.Application.Feed.GetEventFeed;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class FeedEndpoints
{
    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        var feed = app.MapGroup("/feed");

        feed.MapGet("/editions/{editionId:guid}", async (Guid editionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEditionFeedQuery(editionId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        feed.MapGet("/events/{eventId:guid}", async (Guid eventId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEventFeedQuery(eventId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}
