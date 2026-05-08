using ConventionSystem.Application.Common;
using ConventionSystem.Application.Content.Commands.ResetMailTemplate;
using ConventionSystem.Application.Content.Commands.UpdateMailTemplate;
using ConventionSystem.Application.Content.Queries.GetMailTemplate;
using ConventionSystem.Application.Content.Queries.ListMailTemplates;

namespace ConventionSystem.Api.Endpoints;

public static class MailTemplateEndpoints
{
    public static void MapMailTemplateEndpoints(this RouteGroups groups)
    {
        groups.Admin.MapGet("/api/conventions/{conventionId:guid}/mail-templates",
            async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListMailTemplatesQuery(), ct)));

        groups.Admin.MapGet("/api/conventions/{conventionId:guid}/mail-templates/{type}",
            async (string type, ISender sender, CancellationToken ct) =>
            {
                var template = await sender.Send(new GetMailTemplateQuery(type), ct);
                return template is null ? Results.NotFound() : Results.Ok(template);
            });

        groups.Admin.MapPut("/api/conventions/{conventionId:guid}/mail-templates/{type}",
            async (Guid conventionId, string type, UpdateMailTemplateRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateMailTemplateCommand(conventionId, type, request.Subject, request.BodyMarkdown), ct);
                return Results.NoContent();
            });

        groups.Admin.MapPost("/api/conventions/{conventionId:guid}/mail-templates/{type}/reset",
            async (Guid conventionId, string type, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ResetMailTemplateCommand(conventionId, type), ct);
                return Results.NoContent();
            });
    }
}

public sealed record UpdateMailTemplateRequest(string Subject, string BodyMarkdown);
