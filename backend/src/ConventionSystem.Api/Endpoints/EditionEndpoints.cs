using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Convention.Commands.ChangeCategoryResponsible;
using ConventionSystem.Application.Convention.Queries.ListEditionResponsibles;
using ConventionSystem.Application.Convention.Commands.SetActiveEdition;
using ConventionSystem.Application.Event.Queries.ListEditionOrganisers;
using ConventionSystem.Application.Registration.Queries.ListEditionVisitors;
using ConventionSystem.Application.Registration.Queries.ListEditionStaff;
using ConventionSystem.Application.Convention.Commands.CopyEditionStructure;
using ConventionSystem.Application.Convention.Commands.CreateCategory;
using ConventionSystem.Application.Convention.Commands.CreateEdition;
using ConventionSystem.Application.Convention.Commands.CreateStaffArea;
using ConventionSystem.Application.Convention.Commands.CreateStation;
using ConventionSystem.Application.Convention.Commands.CreateVenue;
using ConventionSystem.Application.Convention.Commands.OpenRegistration;
using ConventionSystem.Application.Convention.Commands.PublishEdition;
using ConventionSystem.Application.Convention.Commands.RemoveCategory;
using ConventionSystem.Application.Convention.Commands.RemoveStaffArea;
using ConventionSystem.Application.Convention.Commands.RemoveStation;
using ConventionSystem.Application.Convention.Commands.RemoveVenue;
using ConventionSystem.Application.Convention.Commands.UpdateStation;
using ConventionSystem.Application.Convention.Commands.UpdateCategory;
using ConventionSystem.Application.Convention.Commands.UpdateEdition;
using ConventionSystem.Application.Convention.Commands.UpdateStaffArea;
using ConventionSystem.Application.Convention.Commands.UpdateVenue;
using ConventionSystem.Domain.Convention.Enums;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class EditionEndpoints
{
    public static IEndpointRouteBuilder MapEditionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/editions/{editionId:guid}",
            async (Guid editionId, UpdateEditionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateEditionCommand(
                    editionId,
                    request.Name,
                    request.StartDate,
                    request.EndDate,
                    request.StaffCoordinatorId,
                    request.EventCoordinatorId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPut("/editions/{editionId:guid}/venues/{venueId:guid}",
            async (Guid editionId, Guid venueId, UpdateVenueRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateVenueCommand(editionId, venueId, request.Name, request.Building, request.Description), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapDelete("/editions/{editionId:guid}/venues/{venueId:guid}",
            async (Guid editionId, Guid venueId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveVenueCommand(editionId, venueId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPut("/editions/{editionId:guid}/staff-areas/{staffAreaId:guid}",
            async (Guid editionId, Guid staffAreaId, UpdateStaffAreaRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateStaffAreaCommand(editionId, staffAreaId, request.Name, request.Description, request.ResponsibleId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapDelete("/editions/{editionId:guid}/staff-areas/{staffAreaId:guid}",
            async (Guid editionId, Guid staffAreaId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveStaffAreaCommand(editionId, staffAreaId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapDelete("/editions/{editionId:guid}/categories/{categoryId:guid}",
            async (Guid editionId, Guid categoryId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveCategoryCommand(editionId, categoryId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

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
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/editions/{editionId:guid}/publish",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new PublishEditionCommand(editionId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/editions/{editionId:guid}/copy-structure",
            async (Guid editionId, CopyEditionStructureRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CopyEditionStructureCommand(editionId, request.SourceEditionId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPut("/editions/{editionId:guid}/categories/{categoryId:guid}",
            async (Guid editionId, Guid categoryId, UpdateCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateCategoryCommand(editionId, categoryId, request.Name, request.Description, request.ResponsibleId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/editions/{editionId:guid}/categories",
            async (Guid editionId, CreateCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateCategoryCommand(
                    editionId, request.Name, request.Description, request.ResponsibleId), ct);
                return Results.Created($"/categories/{id}", new { id });
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/editions/{editionId:guid}/staff-areas",
            async (Guid editionId, CreateStaffAreaRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateStaffAreaCommand(
                    editionId, request.Name, request.Description, request.ResponsibleId), ct);
                return Results.Created($"/staff-areas/{id}", new { id });
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/editions/{editionId:guid}/stations",
            async (Guid editionId, CreateStationRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateStationCommand(
                    editionId, request.Name, request.Description, request.StaffAreaId), ct);
                return Results.Created($"/stations/{id}", new { id });
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPut("/editions/{editionId:guid}/stations/{stationId:guid}",
            async (Guid editionId, Guid stationId, UpdateStationRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateStationCommand(editionId, stationId, request.Name, request.Description), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapDelete("/editions/{editionId:guid}/stations/{stationId:guid}",
            async (Guid editionId, Guid stationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveStationCommand(editionId, stationId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/editions/{editionId:guid}/venues",
            async (Guid editionId, CreateVenueRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateVenueCommand(
                    editionId, request.Name, request.Building, request.Description), ct);
                return Results.Created($"/venues/{id}", new { id });
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/editions/{editionId:guid}/registrations/{type}/open",
            async (Guid editionId, string type, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse<RegistrationType>(type, ignoreCase: true, out var registrationType))
                    return Results.BadRequest($"Okänd registreringstyp: {type}.");
                await sender.Send(new OpenRegistrationCommand(editionId, registrationType), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/editions/{editionId:guid}/set-active",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SetActiveEditionCommand(editionId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapGet("/editions/{editionId:guid}/visitors",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionVisitorsQuery(editionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapGet("/editions/{editionId:guid}/organisers",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionOrganisersQuery(editionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapGet("/editions/{editionId:guid}/staff",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionStaffQuery(editionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapGet("/editions/{editionId:guid}/responsibles",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionResponsiblesQuery(editionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        return app;
    }
}

public record CopyEditionStructureRequest(Guid SourceEditionId);
public record CreateVenueRequest(string Name, string Building, string? Description);
public record UpdateVenueRequest(string Name, string Building, string? Description);
public record CreateStaffAreaRequest(string Name, string? Description, Guid ResponsibleId);
public record UpdateStaffAreaRequest(string Name, string? Description, Guid ResponsibleId);
public record CreateStationRequest(string Name, string? Description, Guid StaffAreaId);
public record UpdateStationRequest(string Name, string? Description);
public record CreateCategoryRequest(string Name, string? Description, Guid ResponsibleId);
public record UpdateCategoryRequest(string Name, string? Description, Guid ResponsibleId);
public record ChangeCategoryResponsibleRequest(Guid NewResponsibleId);

public record CreateEditionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId);

public record UpdateEditionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId);
