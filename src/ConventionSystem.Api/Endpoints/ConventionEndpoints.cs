using ConventionSystem.Application.Convention.Commands.AddAdministrator;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class ConventionEndpoints
{
    public static IEndpointRouteBuilder MapConventionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conventions");

        group.MapPost("/", async (CreateConventionRequest request, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new CreateConventionCommand(request.Name, request.Slug, request.RegistrantName, request.RegistrantEmail), ct);
            return Results.Created($"/conventions/{id}", new { id });
        });

        group.MapPost("/{conventionId:guid}/administrators",
            async (Guid conventionId, AddAdministratorRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddAdministratorCommand(conventionId, request.PersonId, request.PerformedById), ct);
                return Results.NoContent();
            });

        return app;
    }
}

public record CreateConventionRequest(string Name, string Slug, string RegistrantName, string RegistrantEmail);
public record AddAdministratorRequest(Guid PersonId, Guid PerformedById);
