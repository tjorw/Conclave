using ConventionSystem.Application.Convention.Commands.SetEditionContent;
using ConventionSystem.Application.Convention.Commands.SetEditionLocales;
using ConventionSystem.Application.Convention.Queries.GetEditionContent;
using ConventionSystem.Application.Convention.Queries.GetEditionLocales;
using ConventionSystem.Application.Reception.Queries.GetPersonScheduleForReception;
using ConventionSystem.Application.Convention.Commands.AddReceptionStaff;
using ConventionSystem.Application.Convention.Commands.RemoveReceptionStaff;
using ConventionSystem.Application.Convention.Queries.ListReceptionStaff;
using ConventionSystem.Application.Convention.Queries.SearchPersonsForReception;
using ConventionSystem.Application.Convention.Commands.ChangeCategoryResponsible;
using ConventionSystem.Application.Convention.Queries.ListEditionResponsibles;
using ConventionSystem.Application.Convention.Commands.SetActiveEdition;
using ConventionSystem.Application.Event.Queries.GetEditionSessions;
using ConventionSystem.Application.Event.Queries.ListEditionOrganisers;
using ConventionSystem.Application.Registration.Queries.ListEditionVisitors;
using ConventionSystem.Application.Registration.Queries.ListEditionStaff;
using ConventionSystem.Application.Convention.Commands.CopyEditionStructure;
using ConventionSystem.Application.Convention.Commands.CreateCategory;
using ConventionSystem.Application.Convention.Commands.CreateEdition;
using ConventionSystem.Application.Convention.Commands.CreateStaffArea;
using ConventionSystem.Application.Convention.Commands.CreateStation;
using ConventionSystem.Application.Convention.Commands.CreateVenue;
using ConventionSystem.Application.Convention.Commands.CloseRegistration;
using ConventionSystem.Application.Convention.Commands.OpenRegistration;
using ConventionSystem.Application.Convention.Commands.PublishEdition;
using ConventionSystem.Application.Convention.Commands.RemoveEdition;
using ConventionSystem.Application.Convention.Commands.UnpublishEdition;
using ConventionSystem.Application.Convention.Commands.RemoveCategory;
using ConventionSystem.Application.Convention.Commands.RemoveStaffArea;
using ConventionSystem.Application.Convention.Commands.RemoveStation;
using ConventionSystem.Application.Convention.Commands.RemoveVenue;
using ConventionSystem.Application.Convention.Commands.UpdateStation;
using ConventionSystem.Application.Convention.Commands.UpdateCategory;
using ConventionSystem.Application.Convention.Commands.UpdateEdition;
using ConventionSystem.Application.Convention.Commands.UpdateStaffArea;
using ConventionSystem.Application.Convention.Commands.UpdateVenue;
using ConventionSystem.Application.Convention.Commands.CreateProgramTagDefinition;
using ConventionSystem.Application.Convention.Commands.UpdateProgramTagDefinition;
using ConventionSystem.Application.Convention.Commands.RemoveProgramTagDefinition;
using ConventionSystem.Application.Export.Commands.ExportEdition;
using ConventionSystem.Application.Export.Commands.ImportEdition;
using ConventionSystem.Application.Export.Contracts;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Staff.Queries.GetStaffSchedule;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConventionSystem.Api.Endpoints;

public static class EditionEndpoints
{
    public static void MapEditionEndpoints(this RouteGroups groups)
    {
        groups.Admin.MapPost("/conventions/{conventionId:guid}/editions",
            async (Guid conventionId, CreateEditionRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateEditionCommand(
                    conventionId,
                    request.Name,
                    request.StartDate,
                    request.EndDate,
                    request.StaffCoordinatorId,
                    request.EventCoordinatorId,
                    request.ScheduleDays?
                        .Select(d => new ConventionSystem.Application.Convention.Commands.CreateEdition.EditionScheduleDayCommand(
                            d.Date, d.StartTime, d.EndTime))
                        .ToList()), ct);
                return Results.Created($"/editions/{id}", new { id });
            });

        groups.Admin.MapPost("/conventions/{conventionId:guid}/editions/import",
            async (Guid conventionId, ImportEditionRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ImportEditionCommand(
                    conventionId,
                    request.Name,
                    request.StartDate,
                    request.Document), ct);

                return Results.Ok(result);
            });

        var editions = groups.Admin.MapGroup("/editions/{editionId:guid}");

        editions.MapPut("/",
            async (Guid editionId, UpdateEditionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateEditionCommand(
                    editionId,
                    request.Name,
                    request.StartDate,
                    request.EndDate,
                    request.StaffCoordinatorId,
                    request.EventCoordinatorId,
                    request.ScheduleDays?
                        .Select(d => new ConventionSystem.Application.Convention.Commands.UpdateEdition.EditionScheduleDayCommand(
                            d.Date, d.StartTime, d.EndTime))
                        .ToList()), ct);
                return Results.NoContent();
            });

        editions.MapDelete("/",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveEditionCommand(editionId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/publish",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new PublishEditionCommand(editionId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/unpublish",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UnpublishEditionCommand(editionId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/copy-structure",
            async (Guid editionId, CopyEditionStructureRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CopyEditionStructureCommand(editionId, request.SourceEditionId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/set-active",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SetActiveEditionCommand(editionId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/registrations/{type}/open",
            async (Guid editionId, string type, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse<RegistrationType>(type, ignoreCase: true, out var registrationType))
                    return Results.BadRequest($"Okänd registreringstyp: {type}.");
                await sender.Send(new OpenRegistrationCommand(editionId, registrationType), ct);
                return Results.NoContent();
            });

        editions.MapPost("/registrations/{type}/close",
            async (Guid editionId, string type, ISender sender, CancellationToken ct) =>
            {
                if (!Enum.TryParse<RegistrationType>(type, ignoreCase: true, out var registrationType))
                    return Results.BadRequest($"Okänd registreringstyp: {type}.");
                await sender.Send(new CloseRegistrationCommand(editionId, registrationType), ct);
                return Results.NoContent();
            });

        editions.MapPost("/event-submissions/open",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new OpenRegistrationCommand(editionId, RegistrationType.Organiser), ct);
                return Results.NoContent();
            });

        editions.MapPost("/event-submissions/close",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CloseRegistrationCommand(editionId, RegistrationType.Organiser), ct);
                return Results.NoContent();
            });

        editions.MapPost("/staff-applications/open",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new OpenRegistrationCommand(editionId, RegistrationType.Staff), ct);
                return Results.NoContent();
            });

        editions.MapPost("/staff-applications/close",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CloseRegistrationCommand(editionId, RegistrationType.Staff), ct);
                return Results.NoContent();
            });

        editions.MapPost("/venues",
            async (Guid editionId, CreateVenueRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateVenueCommand(
                    editionId, request.Name, request.Building, request.Description), ct);
                return Results.Created($"/venues/{id}", new { id });
            });

        editions.MapPut("/venues/{venueId:guid}",
            async (Guid editionId, Guid venueId, UpdateVenueRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateVenueCommand(editionId, venueId, request.Name, request.Building, request.Description), ct);
                return Results.NoContent();
            });

        editions.MapDelete("/venues/{venueId:guid}",
            async (Guid editionId, Guid venueId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveVenueCommand(editionId, venueId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/staff-areas",
            async (Guid editionId, CreateStaffAreaRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateStaffAreaCommand(
                    editionId, request.Name, request.Description, request.ResponsibleId), ct);
                return Results.Created($"/staff-areas/{id}", new { id });
            });

        editions.MapPut("/staff-areas/{staffAreaId:guid}",
            async (Guid editionId, Guid staffAreaId, UpdateStaffAreaRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateStaffAreaCommand(editionId, staffAreaId, request.Name, request.Description, request.ResponsibleId), ct);
                return Results.NoContent();
            });

        editions.MapDelete("/staff-areas/{staffAreaId:guid}",
            async (Guid editionId, Guid staffAreaId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveStaffAreaCommand(editionId, staffAreaId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/stations",
            async (Guid editionId, CreateStationRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateStationCommand(
                    editionId, request.Name, request.Description, request.StaffAreaId), ct);
                return Results.Created($"/stations/{id}", new { id });
            });

        editions.MapPut("/stations/{stationId:guid}",
            async (Guid editionId, Guid stationId, UpdateStationRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateStationCommand(editionId, stationId, request.Name, request.Description), ct);
                return Results.NoContent();
            });

        editions.MapDelete("/stations/{stationId:guid}",
            async (Guid editionId, Guid stationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveStationCommand(editionId, stationId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/categories",
            async (Guid editionId, CreateCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateCategoryCommand(
                    editionId, request.Name, request.OrganizerInstructions, request.PublicDescription, request.ResponsibleId), ct);
                return Results.Created($"/categories/{id}", new { id });
            });

        editions.MapPut("/categories/{categoryId:guid}",
            async (Guid editionId, Guid categoryId, UpdateCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateCategoryCommand(editionId, categoryId, request.Name, request.OrganizerInstructions, request.PublicDescription, request.ResponsibleId), ct);
                return Results.NoContent();
            });

        editions.MapPut("/categories/{categoryId:guid}/responsible",
            async (Guid editionId, Guid categoryId, ChangeCategoryResponsibleRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ChangeCategoryResponsibleCommand(editionId, categoryId, request.NewResponsibleId), ct);
                return Results.NoContent();
            });

        editions.MapDelete("/categories/{categoryId:guid}",
            async (Guid editionId, Guid categoryId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveCategoryCommand(editionId, categoryId), ct);
                return Results.NoContent();
            });

        editions.MapPost("/program-tag-definitions",
            async (Guid editionId, CreateProgramTagDefinitionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CreateProgramTagDefinitionCommand(editionId, request.Name), ct);
                return Results.NoContent();
            });

        editions.MapPut("/program-tag-definitions",
            async (Guid editionId, UpdateProgramTagDefinitionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateProgramTagDefinitionCommand(editionId, request.CurrentName, request.NewName), ct);
                return Results.NoContent();
            });

        editions.MapDelete("/program-tag-definitions/{name}",
            async (Guid editionId, string name, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveProgramTagDefinitionCommand(editionId, name), ct);
                return Results.NoContent();
            });

        editions.MapGet("/reception-staff",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListReceptionStaffQuery(editionId), ct)));

        editions.MapPost("/reception-staff",
            async (Guid editionId, AddReceptionStaffRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddReceptionStaffCommand(editionId, request.PersonId), ct);
                return Results.NoContent();
            });

        editions.MapDelete("/reception-staff/{personId:guid}",
            async (Guid editionId, Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveReceptionStaffCommand(editionId, personId), ct);
                return Results.NoContent();
            });

        editions.MapGet("/visitors",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionVisitorsQuery(editionId), ct)));

        editions.MapGet("/organisers",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionOrganisersQuery(editionId), ct)));

        editions.MapGet("/staff",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionStaffQuery(editionId), ct)));


        editions.MapGet("/export",
            async (Guid editionId, bool? includeEvents, bool? includeTicketTypes, bool? includePages, ISender sender, CancellationToken ct) =>
            {
                var export = await sender.Send(new ExportEditionCommand(
                    editionId,
                    includeEvents ?? false,
                    includeTicketTypes ?? false,
                    includePages ?? false), ct);

                var bytes = JsonSerializer.SerializeToUtf8Bytes(export.Document, ExportJsonOptions);
                return Results.File(bytes, "application/json", export.FileName);
            });

        groups.Anonymous.MapGet("/editions/{editionId:guid}/content",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetEditionContentQuery(editionId), ct)));

        editions.MapPut("/content",
            async (Guid editionId, SetEditionContentRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SetEditionContentCommand(
                    editionId,
                    request.Items.Select(i => new EditionContentItem(i.Key, i.Value)).ToList()), ct);
                return Results.NoContent();
            });

        groups.Authenticated.MapGet("/editions/{editionId:guid}/responsibles",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionResponsiblesQuery(editionId), ct)));

        groups.Authenticated.MapGet("/editions/{editionId:guid}/sessions",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetEditionSessionsQuery(editionId), ct)));

        editions.MapGet("/locales",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetEditionLocalesQuery(editionId), ct)));

        editions.MapPut("/locales",
            async (Guid editionId, SetEditionLocalesRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SetEditionLocalesCommand(editionId, request.Locales, request.PrimaryLocale), ct);
                return Results.NoContent();
            });

        groups.Authenticated.MapGet("/editions/{editionId:guid}/staff-schedule",
            async (Guid editionId, Guid? staffAreaId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetStaffScheduleQuery(editionId, staffAreaId), ct)));

        groups.Authenticated.MapGet("/editions/{editionId:guid}/persons/search",
            async (Guid editionId, string q, ISender sender, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(q))
                    return Results.BadRequest("Sökterm krävs.");
                if (!Guid.TryParse(q.Trim(), out _) && q.Trim().Length < 2)
                    return Results.BadRequest("Söktermen måste vara minst 2 tecken.");
                return Results.Ok(await sender.Send(new SearchPersonsForReceptionQuery(editionId, q), ct));
            });

        groups.Authenticated.MapGet("/editions/{editionId:guid}/persons/{personId:guid}/schedule",
            async (Guid editionId, Guid personId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetPersonScheduleForReceptionQuery(personId, editionId), ct)));
    }

    private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

public record AddReceptionStaffRequest(Guid PersonId);
public record CopyEditionStructureRequest(Guid SourceEditionId);
public record ImportEditionRequest(string Name, DateOnly StartDate, EditionExportDocument Document);
public record CreateVenueRequest(string Name, string Building, string? Description);
public record UpdateVenueRequest(string Name, string Building, string? Description);
public record CreateStaffAreaRequest(string Name, string? Description, Guid ResponsibleId);
public record UpdateStaffAreaRequest(string Name, string? Description, Guid ResponsibleId);
public record CreateStationRequest(string Name, string? Description, Guid StaffAreaId);
public record UpdateStationRequest(string Name, string? Description);
public record CreateCategoryRequest(string Name, string? OrganizerInstructions, string? PublicDescription, Guid ResponsibleId);
public record UpdateCategoryRequest(string Name, string? OrganizerInstructions, string? PublicDescription, Guid ResponsibleId);
public record ChangeCategoryResponsibleRequest(Guid NewResponsibleId);
public record CreateProgramTagDefinitionRequest(string Name);
public record UpdateProgramTagDefinitionRequest(string CurrentName, string NewName);

public record SetEditionContentRequest(IReadOnlyList<EditionContentItemRequest> Items);
public record SetEditionLocalesRequest(IReadOnlyList<string> Locales, string PrimaryLocale);
public record EditionContentItemRequest(string Key, string Value);

public record CreateEditionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId,
    IReadOnlyList<EditionScheduleDayRequest>? ScheduleDays = null);

public record UpdateEditionRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid StaffCoordinatorId,
    Guid EventCoordinatorId,
    IReadOnlyList<EditionScheduleDayRequest>? ScheduleDays = null);

public record EditionScheduleDayRequest(DateOnly Date, TimeOnly? StartTime, TimeOnly? EndTime);
