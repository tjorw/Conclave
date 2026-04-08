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
            var id = await sender.Send(new CreateConventionCommand(request.Name, request.Slug), ct);
            return Results.Created($"/conventions/{id}", new { id });
        });

        return app;
    }
}

public record CreateConventionRequest(string Name, string Slug);
