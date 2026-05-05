using ConventionSystem.Application.Event.Commands.RemoveCoOrganiser;
using ConventionSystem.Application.Event.Commands.AddEventComment;
using ConventionSystem.Application.Event.Commands.AcknowledgeEventComment;
using ConventionSystem.Application.Event.Commands.ChangeCategory;
using ConventionSystem.Application.Event.Commands.ApproveVersion;
using ConventionSystem.Application.Event.Commands.CancelEvent;
using ConventionSystem.Application.Event.Commands.DeleteEvent;
using ConventionSystem.Application.Event.Commands.CreateEvent;
using ConventionSystem.Application.Event.Commands.DeactivateSession;
using ConventionSystem.Application.Event.Commands.UpdateSession;
using ConventionSystem.Application.Event.Commands.EditEventDraft;
using ConventionSystem.Application.Event.Commands.RejectVersion;
using ConventionSystem.Application.Event.Commands.ReturnToDraft;
using ConventionSystem.Application.Event.Commands.RespondToEventComment;
using ConventionSystem.Application.Event.Commands.ScheduleSession;
using ConventionSystem.Application.Event.Commands.SubmitForReview;
using ConventionSystem.Application.Event.Commands.AdjustCoOrganiserLimit;
using ConventionSystem.Application.Event.Commands.CreateCoOrganiserInvitation;
using ConventionSystem.Application.Event.Commands.CancelCoOrganiserInvitation;
using ConventionSystem.Application.Event.Commands.RedeemCoOrganiserInvitation;
using ConventionSystem.Application.Event.Queries.GetEvent;
using ConventionSystem.Application.Event.Queries.GetFeaturedEvents;
using ConventionSystem.Application.Event.Queries.ListEvents;
using ConventionSystem.Application.Event.Queries.ListMyEvents;
using ConventionSystem.Application.Event.Commands.SetFeatured;
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

        groups.Anonymous.MapGet("/events/featured",
            async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetFeaturedEventsQuery(), ct)));

        // --- Inloggade ---

        groups.Authenticated.MapGet("/editions/{editionId:guid}/my-events",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListMyEventsQuery(editionId), ct)));

        // UC-EV001 – Skicka in evenemang
        groups.Authenticated.MapPost("/editions/{editionId:guid}/events",
            async (Guid editionId, CreateEventRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new CreateEventCommand(editionId, request.CategoryId, request.LeadOrganiserId, request.ProgramTags ?? []), ct);
                return Results.Created($"/events/{id}", new { id });
            });

        var events = groups.Authenticated.MapGroup("/events/{eventId:guid}");

        // UC-EV002 – Redigera evenemangsutkast
        events.MapPut("/",
            async (Guid eventId, EditEventDraftRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(
                    new EditEventDraftCommand(eventId, request.Title, request.Description,
                        request.ProgramTags ?? [], request.RegistrationType, request.DropInRules, request.ScheduleRequestText,
                        request.CoOrganiserCount), ct);
                return Results.NoContent();
            });

        // R-CO: Admin justerar godkänt antal medarrangörer
        events.MapPut("/co-organiser-limit",
            async (Guid eventId, AdjustCoOrganiserLimitRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AdjustCoOrganiserLimitCommand(eventId, request.Limit), ct);
                return Results.NoContent();
            });

        // R-CO: Skapa inbjudan
        events.MapPost("/co-organiser-invitations",
            async (Guid eventId, CreateCoOrganiserInvitationRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CreateCoOrganiserInvitationCommand(eventId, request.Email), ct);
                return Results.NoContent();
            });

        // R-CO: Avbryt inbjudan
        events.MapDelete("/co-organiser-invitations/{invitationId:guid}",
            async (Guid eventId, Guid invitationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelCoOrganiserInvitationCommand(eventId, invitationId), ct);
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
            async (Guid eventId, ApproveVersionRequest request, ISender sender, CancellationToken ct) =>
            {
                var assignments = request.OrganizerTicketAssignments ?? [];
                await sender.Send(new ApproveVersionCommand(
                    eventId,
                    assignments.Select(a => new ApproveOrganizerTicketAssignment(a.PersonId, a.TicketTypeId)).ToList()), ct);
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

        groups.Admin.MapPut("/events/{eventId:guid}/featured",
            async (Guid eventId, SetFeaturedRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SetFeaturedCommand(eventId, request.IsFeatured, request.FeaturedSortOrder), ct);
                return Results.NoContent();
            });

        groups.Admin.MapDelete("/events/{eventId:guid}/co-organisers/{personId:guid}",
            async (Guid eventId, Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveCoOrganiserCommand(eventId, personId), ct);
                return Results.NoContent();
            });

        // R-CO: Lös in inbjudan
        groups.Authenticated.MapPost("/co-organiser-invitations/redeem",
            async (RedeemCoOrganiserInvitationRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RedeemCoOrganiserInvitationCommand(request.Code), ct);
                return Results.NoContent();
            });
    }
}

public record CreateEventRequest(Guid CategoryId, Guid LeadOrganiserId, IReadOnlyList<string>? ProgramTags = null);
public record ApproveVersionRequest(IReadOnlyList<ApproveOrganizerTicketAssignmentRequest>? OrganizerTicketAssignments = null);
public record ApproveOrganizerTicketAssignmentRequest(Guid PersonId, Guid? TicketTypeId);
public record ChangeCategoryRequest(Guid CategoryId);
public record EditEventDraftRequest(string Title, string Description, IReadOnlyList<string>? ProgramTags, RegistrationType RegistrationType, string? DropInRules, string? ScheduleRequestText, int CoOrganiserCount);
public record RejectVersionRequest(string Comment);
public record AddEventCommentRequest(string Comment);
public record RespondToEventCommentRequest(string Response);
public record ScheduleSessionRequest(Guid VenueId, DateTime StartTime, DateTime EndTime, int MaxSeats, StartType StartType);
public record UpdateSessionRequest(Guid VenueId, DateTime StartTime, DateTime EndTime, int MaxSeats, StartType StartType);
public record AdjustCoOrganiserLimitRequest(int Limit);
public record CreateCoOrganiserInvitationRequest(string Email);
public record RedeemCoOrganiserInvitationRequest(string Code);
public record SetFeaturedRequest(bool IsFeatured, int? FeaturedSortOrder);
