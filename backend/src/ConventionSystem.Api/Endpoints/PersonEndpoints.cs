using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Convention.Commands.CreatePerson;
using ConventionSystem.Application.Convention.Commands.DeactivatePerson;
using ConventionSystem.Application.Convention.Commands.ReactivatePerson;
using ConventionSystem.Application.Convention.Commands.UpdatePerson;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class PersonEndpoints
{
    public static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/conventions/{conventionId:guid}/persons",
            async (Guid conventionId, CreatePersonRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new CreatePersonCommand(conventionId, request.Name, request.Email, request.Phone), ct);
                return Results.Created($"/persons/{id}", new { id });
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPut("/persons/{personId:guid}",
            async (Guid personId, UpdatePersonRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(
                    new UpdatePersonCommand(personId, request.Name, request.Email, request.Phone), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapDelete("/persons/{personId:guid}",
            async (Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivatePersonCommand(personId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/persons/{personId:guid}/reactivate",
            async (Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ReactivatePersonCommand(personId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        return app;
    }
}

public record CreatePersonRequest(string Name, string Email, string? Phone);
public record UpdatePersonRequest(string Name, string Email, string? Phone);
