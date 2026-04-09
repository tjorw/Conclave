using ConventionSystem.Application.Convention.Commands.CopyEditionStructure;
using ConventionSystem.Application.Convention.Commands.CreateEdition;
using ConventionSystem.Application.Convention.Commands.CreateStation;
using ConventionSystem.Application.Convention.Commands.CreateVenue;
using ConventionSystem.Application.Convention.Commands.OpenRegistration;
using ConventionSystem.Application.Convention.Commands.PublishEdition;
using ConventionSystem.Domain.Convention.Enums;
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

        app.MapPost("/editions/{editionId:guid}/copy-structure",
            async (Guid editionId, CopyEditionStructureRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CopyEditionStructureCommand(editionId, request.SourceEditionId, request.PerformedById), ct);
                return Results.NoContent();
            });

        app.MapPost("/editions/{editionId:guid}/stations",
            async (Guid editionId, CreateStationRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateStationCommand(
                    editionId, request.Name, request.Description, request.ResponsibleId, request.PerformedById), ct);
                return Results.Created($"/stations/{id}", new { id });
            });

        app.MapPost("/editions/{editionId:guid}/venues",
            async (Guid editionId, CreateVenueRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateVenueCommand(
                    editionId, request.Name, request.Building, request.Description, request.PerformedById), ct);
                return Results.Created($"/venues/{id}", new { id });
            });

        app.MapPost("/editions/{editionId:guid}/registrations/{type}/open",
            async (Guid editionId, string type, OpenRegistrationRequest request, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse<RegistrationType>(type, ignoreCase: true, out var registrationType))
                    return Results.BadRequest($"Okänd registreringstyp: {type}.");
                await sender.Send(new OpenRegistrationCommand(editionId, registrationType, request.PerformedById), ct);
                return Results.NoContent();
            });

        return app;
    }
}

public record PublishEditionRequest(Guid PerformedById);
public record CopyEditionStructureRequest(Guid SourceEditionId, Guid PerformedById);
public record OpenRegistrationRequest(Guid PerformedById);
public record CreateVenueRequest(string Name, string Building, string? Description, Guid PerformedById);
public record CreateStationRequest(string Name, string? Description, Guid ResponsibleId, Guid PerformedById);

public record CreateEditionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId,
    Guid PerformedById);
