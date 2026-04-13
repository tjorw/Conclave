using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Event.Commands.AddCoOrganiser;
using ConventionSystem.Application.Event.Commands.ChangeCategory;
using ConventionSystem.Application.Event.Commands.AddSessionRequest;
using ConventionSystem.Application.Event.Commands.ApproveVersion;
using ConventionSystem.Application.Event.Commands.CancelEvent;
using ConventionSystem.Application.Event.Commands.CreateEvent;
using ConventionSystem.Application.Event.Commands.DeactivateSession;
using ConventionSystem.Application.Event.Commands.UpdateSession;
using ConventionSystem.Application.Event.Commands.EditEventDraft;
using ConventionSystem.Application.Event.Commands.RejectVersion;
using ConventionSystem.Application.Event.Commands.RemoveSessionRequest;
using ConventionSystem.Application.Event.Commands.ReturnToDraft;
using ConventionSystem.Application.Event.Commands.ScheduleSession;
using ConventionSystem.Application.Event.Commands.SubmitForReview;
using ConventionSystem.Application.Event.Queries.GetEvent;
using ConventionSystem.Application.Event.Queries.ListEvents;
using ConventionSystem.Domain.Event.Enums;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/editions/{editionId:guid}/events",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEventsQuery(editionId), ct)));

        app.MapGet("/events/{eventId:guid}", async (Guid eventId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEventQuery(eventId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // UC-EV001 – Skicka in evenemang
        app.MapPost("/editions/{editionId:guid}/events",
            async (Guid editionId, CreateEventRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new CreateEventCommand(editionId, request.CategoryId, request.LeadOrganiserId, request.ConventionId), ct);
                return Results.Created($"/events/{id}", new { id });
            }).RequireAuthorization();

        app.MapPut("/events/{eventId:guid}/category",
            async (Guid eventId, ChangeCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ChangeCategoryCommand(eventId, request.CategoryId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // UC-EV002 – Redigera evenemangsutkast
        app.MapPut("/events/{eventId:guid}",
            async (Guid eventId, EditEventDraftRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(
                    new EditEventDraftCommand(eventId, request.Title, request.Description,
                        request.RegistrationType, request.DropInRules), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV003 – Lägg till sessionönskemål
        app.MapPost("/events/{eventId:guid}/session-requests",
            async (Guid eventId, AddSessionRequestRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new AddSessionRequestCommand(eventId, request.Description,
                        request.DurationMinutes, request.Seats, request.StartType), ct);
                return Results.Created($"/session-requests/{id}", new { id });
            }).RequireAuthorization();

        // UC-EV004 – Ta bort sessionönskemål
        app.MapDelete("/events/{eventId:guid}/session-requests/{requestId:guid}",
            async (Guid eventId, Guid requestId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveSessionRequestCommand(eventId, requestId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV005 – Lägg till medarrangör
        app.MapPost("/events/{eventId:guid}/co-organisers",
            async (Guid eventId, AddCoOrganiserRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddCoOrganiserCommand(eventId, request.PersonId, request.ConventionId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV006 – Skicka in för granskning
        app.MapPost("/events/{eventId:guid}/submit",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SubmitForReviewCommand(eventId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV007 – Godkänn evenemangsversion
        app.MapPost("/events/{eventId:guid}/approve",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ApproveVersionCommand(eventId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV008b – Återställ till utkast (admin)
        app.MapPost("/events/{eventId:guid}/return-to-draft",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ReturnToDraftCommand(eventId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV008 – Avvisa evenemangsversion
        app.MapPost("/events/{eventId:guid}/reject",
            async (Guid eventId, RejectVersionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RejectVersionCommand(eventId, request.Comment), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV009 – Schemalägg session
        app.MapPost("/events/{eventId:guid}/sessions",
            async (Guid eventId, ScheduleSessionRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new ScheduleSessionCommand(eventId, request.VenueId, request.StartTime, request.EndTime,
                        request.MaxSeats, request.StartType), ct);
                return Results.Created($"/sessions/{id}", new { id });
            }).RequireAuthorization();

        // UC-EV009b – Redigera session
        app.MapPut("/events/{eventId:guid}/sessions/{sessionId:guid}",
            async (Guid eventId, Guid sessionId, UpdateSessionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(
                    new UpdateSessionCommand(eventId, sessionId, request.VenueId, request.StartTime, request.EndTime,
                        request.MaxSeats, request.StartType), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV010 – Inaktivera session
        app.MapPost("/events/{eventId:guid}/sessions/{sessionId:guid}/deactivate",
            async (Guid eventId, Guid sessionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateSessionCommand(eventId, sessionId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-EV011 – Ställ in evenemang
        app.MapPost("/events/{eventId:guid}/cancel",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelEventCommand(eventId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        return app;
    }
}

public record CreateEventRequest(Guid CategoryId, Guid LeadOrganiserId, Guid ConventionId);
public record ChangeCategoryRequest(Guid CategoryId);
public record EditEventDraftRequest(string Title, string Description, RegistrationType RegistrationType, string? DropInRules);
public record AddSessionRequestRequest(string Description, int DurationMinutes, int Seats, StartType StartType);
public record AddCoOrganiserRequest(Guid PersonId, Guid ConventionId);
public record RejectVersionRequest(string Comment);
public record ScheduleSessionRequest(Guid VenueId, DateTime StartTime, DateTime EndTime, int MaxSeats, StartType StartType);
public record UpdateSessionRequest(Guid VenueId, DateTime StartTime, DateTime EndTime, int MaxSeats, StartType StartType);
