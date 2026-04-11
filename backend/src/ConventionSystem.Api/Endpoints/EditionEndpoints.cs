using ConventionSystem.Application.Convention.Commands.ChangeCategoryResponsible;
using ConventionSystem.Application.Convention.Commands.CopyEditionStructure;
using ConventionSystem.Application.Convention.Commands.CreateCategory;
using ConventionSystem.Application.Convention.Commands.CreateEdition;
using ConventionSystem.Application.Convention.Commands.CreateStaffArea;
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
                    request.EventCoordinatorId), ct);
                return Results.Created($"/editions/{id}", new { id });
            }).RequireAuthorization("IsAdmin");

        app.MapPost("/editions/{editionId:guid}/publish",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new PublishEditionCommand(editionId), ct);
                return Results.NoContent();
            }).RequireAuthorization("IsAdmin");

        app.MapPost("/editions/{editionId:guid}/copy-structure",
            async (Guid editionId, CopyEditionStructureRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CopyEditionStructureCommand(editionId, request.SourceEditionId), ct);
                return Results.NoContent();
            }).RequireAuthorization("IsAdmin");

        app.MapPut("/editions/{editionId:guid}/categories/{categoryId:guid}",
            async (Guid editionId, Guid categoryId, ChangeCategoryResponsibleRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ChangeCategoryResponsibleCommand(editionId, categoryId, request.NewResponsibleId), ct);
                return Results.NoContent();
            }).RequireAuthorization("IsAdmin");

        app.MapPost("/editions/{editionId:guid}/categories",
            async (Guid editionId, CreateCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateCategoryCommand(
                    editionId, request.Name, request.Description, request.ResponsibleId), ct);
                return Results.Created($"/categories/{id}", new { id });
            }).RequireAuthorization("IsAdmin");

        app.MapPost("/editions/{editionId:guid}/staff-areas",
            async (Guid editionId, CreateStaffAreaRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateStaffAreaCommand(
                    editionId, request.Name, request.Description, request.ResponsibleId), ct);
                return Results.Created($"/staff-areas/{id}", new { id });
            }).RequireAuthorization("IsAdmin");

        app.MapPost("/editions/{editionId:guid}/stations",
            async (Guid editionId, CreateStationRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateStationCommand(
                    editionId, request.Name, request.Description, request.StaffAreaId), ct);
                return Results.Created($"/stations/{id}", new { id });
            }).RequireAuthorization("IsAdmin");

        app.MapPost("/editions/{editionId:guid}/venues",
            async (Guid editionId, CreateVenueRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateVenueCommand(
                    editionId, request.Name, request.Building, request.Description), ct);
                return Results.Created($"/venues/{id}", new { id });
            }).RequireAuthorization("IsAdmin");

        app.MapPost("/editions/{editionId:guid}/registrations/{type}/open",
            async (Guid editionId, string type, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse<RegistrationType>(type, ignoreCase: true, out var registrationType))
                    return Results.BadRequest($"Okänd registreringstyp: {type}.");
                await sender.Send(new OpenRegistrationCommand(editionId, registrationType), ct);
                return Results.NoContent();
            }).RequireAuthorization("IsAdmin");

        return app;
    }
}

public record CopyEditionStructureRequest(Guid SourceEditionId);
public record CreateVenueRequest(string Name, string Building, string? Description);
public record CreateStaffAreaRequest(string Name, string? Description, Guid ResponsibleId);
public record CreateStationRequest(string Name, string? Description, Guid StaffAreaId);
public record CreateCategoryRequest(string Name, string? Description, Guid ResponsibleId);
public record ChangeCategoryResponsibleRequest(Guid NewResponsibleId);

public record CreateEditionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId);
