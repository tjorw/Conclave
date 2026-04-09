using ConventionSystem.Application.Convention.Commands.CreateEdition;
using ConventionSystem.Application.Convention.Commands.PublishEdition;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class EditionEndpoints
{
    public static IEndpointRouteBuilder MapEditionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/conventions/{conventionId:guid}/editions",
            async (Guid conventionId, CreateEditionRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateEditionCommand(
                    conventionId,
                    request.Name,
                    request.StartDate,
                    request.EndDate,
                    request.StaffCoordinatorId,
                    request.EventCoordinatorId,
                    request.PerformedById), ct);
                return Results.Created($"/editions/{id}", new { id });
            });

        app.MapPost("/editions/{editionId:guid}/publish",
            async (Guid editionId, PublishEditionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new PublishEditionCommand(editionId, request.PerformedById), ct);
                return Results.NoContent();
            });

        return app;
    }
}

public record PublishEditionRequest(Guid PerformedById);

public record CreateEditionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId,
    Guid PerformedById);
