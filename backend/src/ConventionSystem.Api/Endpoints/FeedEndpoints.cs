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
            HttpContext httpContext, Guid editionId, string? locale, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEditionFeedQuery(editionId, locale), ct);
            if (result is null) return Results.NotFound();
            httpContext.Response.Headers.CacheControl = "public, max-age=60";
            return Results.Ok(result);
        });

        feed.MapGet("/events/{eventId:guid}", async (
            HttpContext httpContext, Guid eventId, string? locale, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEventFeedQuery(eventId, locale), ct);
            if (result is null) return Results.NotFound();
            httpContext.Response.Headers.CacheControl = "public, max-age=60";
            return Results.Ok(result);
        });

        feed.MapGet("/active-edition", async (
            HttpContext httpContext, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetActiveEditionFeedQuery(), ct);
            if (result is null) return Results.NotFound();
            httpContext.Response.Headers.CacheControl = "public, max-age=30";
            return Results.Ok(result);
        });

    }
}
