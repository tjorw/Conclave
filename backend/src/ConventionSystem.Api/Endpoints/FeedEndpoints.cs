using ConventionSystem.Application.Feed.GetActiveEditionFeed;
using ConventionSystem.Application.Feed.GetEditionFeed;
using ConventionSystem.Application.Feed.GetEventFeed;
using ConventionSystem.Infrastructure.MultiTenancy;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class FeedEndpoints
{
    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        var feed = app.MapGroup("/feed/{conventionId:guid}");

        feed.MapGet("/editions/{editionId:guid}", async (
            Guid editionId, ITenantContext tenantContext, ISender sender, CancellationToken ct) =>
        {
            if (!tenantContext.IsResolved) return Results.NotFound();
            var result = await sender.Send(new GetEditionFeedQuery(editionId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        feed.MapGet("/events/{eventId:guid}", async (
            Guid eventId, ITenantContext tenantContext, ISender sender, CancellationToken ct) =>
        {
            if (!tenantContext.IsResolved) return Results.NotFound();
            var result = await sender.Send(new GetEventFeedQuery(eventId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        feed.MapGet("/active-edition", async (
            ITenantContext tenantContext, ISender sender, CancellationToken ct) =>
        {
            if (!tenantContext.IsResolved) return Results.NotFound();
            var result = await sender.Send(new GetActiveEditionFeedQuery(), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}
