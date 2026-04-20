using ConventionSystem.Application.Event.Commands.AddCoOrganiser;
using ConventionSystem.Application.Event.Commands.AddEventComment;
using ConventionSystem.Application.Event.Commands.AcknowledgeEventComment;
using ConventionSystem.Application.Event.Commands.ChangeCategory;
using ConventionSystem.Application.Event.Commands.AddSessionRequest;
using ConventionSystem.Application.Event.Commands.ApproveVersion;
using ConventionSystem.Application.Event.Commands.CancelEvent;
using ConventionSystem.Application.Event.Commands.DeleteEvent;
using ConventionSystem.Application.Event.Commands.CreateEvent;
using ConventionSystem.Application.Event.Commands.DeactivateSession;
using ConventionSystem.Application.Event.Commands.UpdateSession;
using ConventionSystem.Application.Event.Commands.EditEventDraft;
using ConventionSystem.Application.Event.Commands.RejectVersion;
using ConventionSystem.Application.Event.Commands.RemoveSessionRequest;
using ConventionSystem.Application.Event.Commands.ReturnToDraft;
using ConventionSystem.Application.Event.Commands.RespondToEventComment;
using ConventionSystem.Application.Event.Commands.ScheduleSession;
using ConventionSystem.Application.Event.Commands.SubmitForReview;
using ConventionSystem.Application.Event.Queries.GetEvent;
using ConventionSystem.Application.Event.Queries.ListEvents;
using ConventionSystem.Application.Event.Queries.ListMyEvents;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Application.Common;

namespace ConventionSystem.Api.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this RouteGroups groups)
    {
        // --- Anonyma ---

        groups.Anonymous.MapGet("/editions/{editionId:guid}/events",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEventsQuery(editionId), ct)));

        groups.Anonymous.MapGet("/events/{eventId:guid}", async (Guid eventId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEventQuery(eventId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // --- Inloggade ---

        groups.Authenticated.MapGet("/editions/{editionId:guid}/my-events",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListMyEventsQuery(editionId), ct)));

        // UC-EV001 – Skicka in evenemang
        groups.Authenticated.MapPost("/editions/{editionId:guid}/events",
            async (Guid editionId, CreateEventRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new CreateEventCommand(editionId, request.CategoryId, request.LeadOrganiserId, request.ConventionId), ct);
                return Results.Created($"/events/{id}", new { id });
            });

        var events = groups.Authenticated.MapGroup("/events/{eventId:guid}");

        // UC-EV002 – Redigera evenemangsutkast
        events.MapPut("/",
            async (Guid eventId, EditEventDraftRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(
                    new EditEventDraftCommand(eventId, request.Title, request.Description,
                        request.RegistrationType, request.DropInRules), ct);
                return Results.NoContent();
            });

        // UC-EV003 – Lägg till sessionönskemål
        events.MapPost("/session-requests",
            async (Guid eventId, AddSessionRequestRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new AddSessionRequestCommand(eventId, request.Description,
                        request.DurationMinutes, request.Seats, request.StartType), ct);
                return Results.Created($"/session-requests/{id}", new { id });
            });

        // UC-EV004 – Ta bort sessionönskemål
        events.MapDelete("/session-requests/{requestId:guid}",
            async (Guid eventId, Guid requestId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveSessionRequestCommand(eventId, requestId), ct);
                return Results.NoContent();
            });

        // UC-EV005 – Lägg till medarrangör
        events.MapPost("/co-organisers",
            async (Guid eventId, AddCoOrganiserRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddCoOrganiserCommand(eventId, request.PersonId, request.ConventionId), ct);
                return Results.NoContent();
            });

        // UC-EV006 – Skicka in för granskning
        events.MapPost("/submit",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SubmitForReviewCommand(eventId), ct);
                return Results.NoContent();
            });

        // 3.1.6b – Arrangör lämnar ändringsförslag via kommentar
        events.MapPost("/comments",
            async (Guid eventId, AddEventCommentRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddEventCommentCommand(eventId, request.Comment), ct);
                return Results.NoContent();
            });

        // 3.1.6b – Admin svarar och markerar kommentar som behandlad
        events.MapPost("/comments/{commentId:guid}/respond",
            async (Guid eventId, Guid commentId, RespondToEventCommentRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RespondToEventCommentCommand(eventId, commentId, request.Response), ct);
                return Results.NoContent();
            });

        // 3.1.6b – Arrangör kvitterar admins svar
        events.MapPost("/comments/{commentId:guid}/acknowledge",
            async (Guid eventId, Guid commentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AcknowledgeEventCommentCommand(eventId, commentId), ct);
                return Results.NoContent();
            });

        // UC-EV007 – Godkänn evenemangsversion
        events.MapPost("/approve",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ApproveVersionCommand(eventId), ct);
                return Results.NoContent();
            });

        // UC-EV008b – Återställ till utkast
        events.MapPost("/return-to-draft",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ReturnToDraftCommand(eventId), ct);
                return Results.NoContent();
            });

        // UC-EV008 – Avvisa evenemangsversion
        events.MapPost("/reject",
            async (Guid eventId, RejectVersionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RejectVersionCommand(eventId, request.Comment), ct);
                return Results.NoContent();
            });

        // UC-EV009 – Schemalägg session
        events.MapPost("/sessions",
            async (Guid eventId, ScheduleSessionRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new ScheduleSessionCommand(eventId, request.VenueId, request.StartTime, request.EndTime,
                        request.MaxSeats, request.StartType), ct);
                return Results.Created($"/sessions/{id}", new { id });
            });

        // UC-EV009b – Redigera session
        events.MapPut("/sessions/{sessionId:guid}",
            async (Guid eventId, Guid sessionId, UpdateSessionRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(
                    new UpdateSessionCommand(eventId, sessionId, request.VenueId, request.StartTime, request.EndTime,
                        request.MaxSeats, request.StartType), ct);
                return Results.NoContent();
            });

        // UC-EV010 – Inaktivera session
        events.MapPost("/sessions/{sessionId:guid}/deactivate",
            async (Guid eventId, Guid sessionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateSessionCommand(eventId, sessionId), ct);
                return Results.NoContent();
            });

        // UC-EV011 – Ställ in evenemang
        events.MapPost("/cancel",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelEventCommand(eventId), ct);
                return Results.NoContent();
            });

        // Ta bort evenemang (Draft eller Cancelled)
        events.MapDelete("/",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeleteEventCommand(eventId), ct);
                return Results.NoContent();
            });

        // --- Admin ---

        groups.Admin.MapPut("/events/{eventId:guid}/category",
            async (Guid eventId, ChangeCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ChangeCategoryCommand(eventId, request.CategoryId), ct);
                return Results.NoContent();
            });

        groups.Admin.MapDelete("/events/{eventId:guid}/sessions/{sessionId:guid}",
            async (Guid eventId, Guid sessionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateSessionCommand(eventId, sessionId), ct);
                return Results.NoContent();
            });
    }
}

public record CreateEventRequest(Guid CategoryId, Guid LeadOrganiserId, Guid ConventionId);
public record ChangeCategoryRequest(Guid CategoryId);
public record EditEventDraftRequest(string Title, string Description, RegistrationType RegistrationType, string? DropInRules);
public record AddSessionRequestRequest(string Description, int DurationMinutes, int Seats, StartType StartType);
public record AddCoOrganiserRequest(Guid PersonId, Guid ConventionId);
public record RejectVersionRequest(string Comment);
public record AddEventCommentRequest(string Comment);
public record RespondToEventCommentRequest(string Response);
public record ScheduleSessionRequest(Guid VenueId, DateTime StartTime, DateTime EndTime, int MaxSeats, StartType StartType);
public record UpdateSessionRequest(Guid VenueId, DateTime StartTime, DateTime EndTime, int MaxSeats, StartType StartType);
