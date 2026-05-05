using ConventionSystem.Application.Convention.Commands.AddAdministrator;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Application.Convention.Commands.RemoveAdministrator;
using ConventionSystem.Application.Convention.Commands.SetConventionBranding;
using ConventionSystem.Application.Convention.Queries.GetConventionBranding;
using ConventionSystem.Application.Convention.Queries.GetConvention;
using ConventionSystem.Application.Convention.Queries.GetEdition;
using ConventionSystem.Application.Convention.Queries.ListEditions;
using ConventionSystem.Application.Convention.Queries.ListPersons;
using ConventionSystem.Application.Common;

namespace ConventionSystem.Api.Endpoints;

public static class ConventionEndpoints
{
    public static void MapConventionEndpoints(this RouteGroups groups)
    {
        // Hämtar den enda konventionen för denna instans – ingen ID krävs (deploy-per-konvention)
        groups.Anonymous.MapGet("/convention", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCurrentConventionQuery(), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        groups.Authenticated.MapPost("/conventions",
            async (CreateConventionRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateConventionCommand(request.Name, request.Slug, request.RegistrantName, request.RegistrantEmail), ct);
                return Results.Created($"/conventions/{id}", new { id });
            });

        groups.Anonymous.MapGet("/conventions/{conventionId:guid}", async (Guid conventionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetConventionQuery(conventionId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        groups.Anonymous.MapGet("/conventions/{conventionId:guid}/branding", async (
            Guid conventionId,
            HttpResponse response,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetConventionBrandingQuery(conventionId), ct);
            if (result is null)
                return Results.NotFound();

            response.Headers.CacheControl = "max-age=300";
            return Results.Ok(result);
        });

        groups.Admin.MapPut("/conventions/{conventionId:guid}/branding",
            async (Guid conventionId, SetConventionBrandingRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SetConventionBrandingCommand(
                    conventionId,
                    request.PrimaryColor,
                    request.AccentColor,
                    request.LogoUrl,
                    request.FaviconUrl,
                    request.FontFamily,
                    request.CustomCss),
                    ct);

                return Results.NoContent();
            });

        groups.Anonymous.MapGet("/conventions/{conventionId:guid}/editions", async (Guid conventionId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListEditionsQuery(conventionId), ct)));

        groups.Admin.MapGet("/conventions/{conventionId:guid}/persons", async (Guid conventionId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListPersonsQuery(conventionId), ct)));

        groups.Admin.MapPost("/conventions/{conventionId:guid}/administrators",
            async (Guid conventionId, AddAdministratorRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddAdministratorCommand(conventionId, request.PersonId), ct);
                return Results.NoContent();
            });

        groups.Admin.MapDelete("/conventions/{conventionId:guid}/administrators/{personId:guid}",
            async (Guid conventionId, Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveAdministratorCommand(conventionId, personId), ct);
                return Results.NoContent();
            });

        groups.Anonymous.MapGet("/editions/{editionId:guid}", async (Guid editionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEditionQuery(editionId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}

public record CreateConventionRequest(string Name, string Slug, string RegistrantName, string RegistrantEmail);
public record AddAdministratorRequest(Guid PersonId);
public record SetConventionBrandingRequest(
    string PrimaryColor,
    string AccentColor,
    string? LogoUrl,
    string? FaviconUrl,
    string FontFamily,
    string? CustomCss);
