using ConventionSystem.Application.Common;
using ConventionSystem.Application.Content.Commands.CreatePage;
using ConventionSystem.Application.Content.Commands.DeletePage;
using ConventionSystem.Application.Content.Commands.PublishPage;
using ConventionSystem.Application.Content.Commands.SetPageTranslation;
using ConventionSystem.Application.Content.Commands.UnpublishPage;
using ConventionSystem.Application.Content.Commands.UpdatePage;
using ConventionSystem.Application.Content.Commands.UpdatePageMenuOrder;
using ConventionSystem.Application.Content.Queries.GetPage;
using ConventionSystem.Application.Content.Queries.GetPageTranslation;
using ConventionSystem.Application.Content.Queries.GetPublicPage;
using ConventionSystem.Application.Content.Queries.ListPublicMenuPages;
using ConventionSystem.Application.Content.Queries.ListPages;

namespace ConventionSystem.Api.Endpoints;

public static class PageEndpoints
{
    public static void MapPageEndpoints(this RouteGroups groups)
    {
        groups.Anonymous.MapGet("/api/pages/menu",
            async (string? locale, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListPublicMenuPagesQuery(locale), ct)));

        groups.Anonymous.MapGet("/api/pages/{slug}",
            async (string slug, string? locale, ISender sender, CancellationToken ct) =>
            {
                var page = await sender.Send(new GetPublicPageQuery(slug, locale), ct);
                return page is null ? Results.NotFound() : Results.Ok(page);
            });

        groups.Admin.MapGet("/api/pages",
            async (Guid? editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListPagesQuery(editionId), ct)));

        groups.Admin.MapGet("/api/pages/{pageId:guid}",
            async (Guid pageId, ISender sender, CancellationToken ct) =>
            {
                var page = await sender.Send(new GetPageQuery(pageId), ct);
                return page is null ? Results.NotFound() : Results.Ok(page);
            });

        groups.Admin.MapPost("/api/pages",
            async (SavePageRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreatePageCommand(request.Slug, request.Title, request.Content, request.EditionId, request.ShowInPublicMenu), ct);
                return Results.Created($"/api/pages/{id}", new { id });
            });

        groups.Admin.MapPut("/api/pages/{pageId:guid}",
            async (Guid pageId, SavePageRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdatePageCommand(pageId, request.Slug, request.Title, request.Content, request.EditionId, request.ShowInPublicMenu), ct);
                return Results.NoContent();
            });

        groups.Admin.MapPatch("/api/pages/{pageId:guid}/menu-order",
            async (Guid pageId, UpdatePageMenuOrderRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdatePageMenuOrderCommand(pageId, request.MenuSortOrder), ct);
                return Results.NoContent();
            });

        groups.Admin.MapPost("/api/pages/{pageId:guid}/publish",
            async (Guid pageId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new PublishPageCommand(pageId), ct);
                return Results.NoContent();
            });

        groups.Admin.MapPost("/api/pages/{pageId:guid}/unpublish",
            async (Guid pageId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UnpublishPageCommand(pageId), ct);
                return Results.NoContent();
            });

        groups.Admin.MapDelete("/api/pages/{pageId:guid}",
            async (Guid pageId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeletePageCommand(pageId), ct);
                return Results.NoContent();
            });

        groups.Admin.MapGet("/api/pages/{pageId:guid}/translations/{locale}",
            async (Guid pageId, string locale, ISender sender, CancellationToken ct) =>
            {
                var translation = await sender.Send(new GetPageTranslationQuery(pageId, locale), ct);
                return translation is null ? Results.NotFound() : Results.Ok(translation);
            });

        groups.Admin.MapPut("/api/pages/{pageId:guid}/translations/{locale}",
            async (Guid pageId, string locale, SavePageTranslationRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SetPageTranslationCommand(pageId, locale, request.Title, request.Content), ct);
                return Results.NoContent();
            });
    }
}

public sealed record SavePageRequest(string Slug, string Title, string Content, Guid? EditionId, bool ShowInPublicMenu);
public sealed record UpdatePageMenuOrderRequest(int MenuSortOrder);
public sealed record SavePageTranslationRequest(string Title, string Content);
