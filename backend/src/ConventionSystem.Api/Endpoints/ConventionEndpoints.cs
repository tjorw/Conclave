using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Convention.Commands.AddAdministrator;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Application.Convention.Commands.RemoveAdministrator;
using ConventionSystem.Application.Convention.Queries.GetConvention;
using ConventionSystem.Application.Convention.Queries.GetEdition;
using ConventionSystem.Application.Convention.Queries.ListEditions;
using ConventionSystem.Application.Convention.Queries.ListPersons;
using MediatR;


namespace ConventionSystem.Api.Endpoints;

public static class ConventionEndpoints
{
    public static IEndpointRouteBuilder MapConventionEndpoints(this IEndpointRouteBuilder app)
    {
        // Hämtar den enda konventionen för denna instans – ingen ID krävs (deploy-per-konvention)
        app.MapGet("/convention", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCurrentConventionQuery(), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        var group = app.MapGroup("/conventions");

        group.MapPost("/", async (CreateConventionRequest request, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new CreateConventionCommand(request.Name, request.Slug, request.RegistrantName, request.RegistrantEmail), ct);
            return Results.Created($"/conventions/{id}", new { id });
        }).RequireAuthorization();

        group.MapGet("/{conventionId:guid}", async (Guid conventionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetConventionQuery(conventionId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/{conventionId:guid}/editions", async (Guid conventionId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListEditionsQuery(conventionId), ct)));

        group.MapGet("/{conventionId:guid}/persons", async (Guid conventionId, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListPersonsQuery(conventionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        group.MapPost("/{conventionId:guid}/administrators",
            async (Guid conventionId, AddAdministratorRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddAdministratorCommand(conventionId, request.PersonId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        group.MapDelete("/{conventionId:guid}/administrators/{personId:guid}",
            async (Guid conventionId, Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveAdministratorCommand(conventionId, personId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        var editions = app.MapGroup("/editions");

        editions.MapGet("/{editionId:guid}", async (Guid editionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEditionQuery(editionId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}

public record CreateConventionRequest(string Name, string Slug, string RegistrantName, string RegistrantEmail);
public record AddAdministratorRequest(Guid PersonId);
