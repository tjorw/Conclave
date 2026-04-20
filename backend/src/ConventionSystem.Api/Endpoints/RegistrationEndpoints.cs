using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Registration.Commands.AcceptStaffApplication;
using ConventionSystem.Application.Registration.Commands.AddAvailability;
using ConventionSystem.Application.Registration.Commands.AddStaffMember;
using ConventionSystem.Application.Registration.Commands.AddStationPreference;
using ConventionSystem.Application.Registration.Commands.CancelSessionRegistration;
using ConventionSystem.Application.Registration.Commands.CancelOwnTicket;
using ConventionSystem.Application.Registration.Commands.CancelVisitorRegistration;
using ConventionSystem.Application.Registration.Commands.CollectTicket;
using ConventionSystem.Application.Registration.Commands.ConfirmTicketPaymentWebhook;
using ConventionSystem.Application.Registration.Commands.ConfirmVisitorRegistrationPayment;
using ConventionSystem.Application.Registration.Commands.CreatePromotionCode;
using ConventionSystem.Application.Registration.Commands.CreateTicketType;
using ConventionSystem.Application.Registration.Commands.DeactivatePromotionCode;
using ConventionSystem.Application.Registration.Commands.DeleteTicketType;
using ConventionSystem.Application.Registration.Commands.IssueTicket;
using ConventionSystem.Application.Registration.Commands.RedeemPromotionCode;
using ConventionSystem.Application.Registration.Commands.RegisterForSession;
using ConventionSystem.Application.Registration.Commands.RegisterManualTicketPayment;
using ConventionSystem.Application.Registration.Commands.RejectStaffApplication;
using ConventionSystem.Application.Registration.Commands.RemoveAvailability;
using ConventionSystem.Application.Registration.Commands.RemoveStationPreference;
using ConventionSystem.Application.Registration.Commands.RevokeTicket;
using ConventionSystem.Application.Registration.Commands.SubmitStaffApplication;
using ConventionSystem.Application.Registration.Commands.SubmitVisitorRegistration;
using ConventionSystem.Application.Registration.Commands.UnwatchSession;
using ConventionSystem.Application.Registration.Commands.UpdateTicketType;
using ConventionSystem.Application.Registration.Commands.WatchSession;
using ConventionSystem.Application.Registration.Queries.GetMySessionRegistrations;
using ConventionSystem.Application.Registration.Queries.GetMyStaffApplication;
using ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;
using ConventionSystem.Application.Registration.Queries.GetMyAssignedShifts;
using ConventionSystem.Application.Registration.Queries.GetMyOrganiserSessions;
using ConventionSystem.Application.Registration.Queries.GetMyWatchedSessions;
using ConventionSystem.Application.Registration.Queries.ListAvailableTicketTypes;
using ConventionSystem.Application.Registration.Queries.ListPromotionCodeRedemptions;
using ConventionSystem.Application.Registration.Queries.ListPromotionCodes;
using ConventionSystem.Application.Registration.Queries.ListTicketTypes;
using ConventionSystem.Application.Registration.Queries.ListVisitorRegistrations;
using ConventionSystem.Application.Staff.Queries.ListStaffApplications;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Application.Common;

namespace ConventionSystem.Api.Endpoints;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        // UC-TK001: Skapa biljetttyp
        app.MapPost("/editions/{editionId:guid}/ticket-types",
            async (Guid editionId, CreateTicketTypeRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateTicketTypeCommand(editionId, request.Name, request.Price, request.Category,
                    request.ValidDays, request.AllowedCategories), ct);
                return Results.Created($"/ticket-types/{id}", new { id });
            }).RequireAuthorization();

        // UC-PC001: Skapa kampanjkod
        app.MapPost("/editions/{editionId:guid}/promotion-codes",
            async (Guid editionId, CreatePromotionCodeRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreatePromotionCodeCommand(
                    editionId,
                    request.Code,
                    request.Description,
                    request.DiscountType,
                    request.DiscountValue,
                    request.MaxRedemptions,
                    request.ValidFrom,
                    request.ValidUntil,
                    request.AllowedTicketTypeIds), ct);
                return Results.Created($"/promotion-codes/{id}", new { id });
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // UC-VR001: Anmäl som besökare
        app.MapPost("/editions/{editionId:guid}/visitor-registrations",
            async (Guid editionId, SubmitVisitorRegistrationRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new SubmitVisitorRegistrationCommand(editionId, request.TicketTypeId), ct);
                return Results.Created($"/visitor-registrations/{id}", new { id });
            }).RequireAuthorization();

        // UC-VR002: Bekräfta betalning
        app.MapPost("/visitor-registrations/{registrationId:guid}/confirm-payment",
            async (Guid registrationId, ConfirmPaymentRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ConfirmVisitorRegistrationPaymentCommand(registrationId, request.ExternalReference), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // UC-TK004: Registrera manuell betalning
        app.MapPost("/tickets/{ticketId:guid}/manual-payment",
            async (Guid ticketId, ManualTicketPaymentRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RegisterManualTicketPaymentCommand(ticketId, request.ExternalReference), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // UC-PC003: Lös in kampanjkod
        app.MapPost("/tickets/{ticketId:guid}/redeem-promotion-code",
            async (Guid ticketId, RedeemPromotionCodeRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RedeemPromotionCodeCommand(ticketId, request.Code), ct);
                return Results.Ok(result);
            }).RequireAuthorization();

        // UC-TK005: Bekräfta betalning via webhook
        app.MapPost("/payments/webhook/tickets",
            async (TicketPaymentWebhookRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ConfirmTicketPaymentWebhookCommand(
                    request.VisitorRegistrationId,
                    request.ExternalReference,
                    request.PaymentStatus), ct);
                return Results.NoContent();
            }).AllowAnonymous();

        // UC-TK006: Avboka biljett (innehavare)
        app.MapDelete("/my/tickets/{ticketId:guid}",
            async (Guid ticketId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelOwnTicketCommand(ticketId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-VR003: Avboka registrering
        app.MapDelete("/visitor-registrations/{registrationId:guid}",
            async (Guid registrationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelVisitorRegistrationCommand(registrationId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-TK002: Utfärda biljett manuellt
        app.MapPost("/editions/{editionId:guid}/tickets",
            async (Guid editionId, IssueTicketRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new IssueTicketCommand(request.PersonId, editionId, request.TicketTypeId), ct);
                return Results.Created($"/tickets/{id}", new { id });
            }).RequireAuthorization();

        // UC-TK008: Hämta ut biljett
        app.MapPost("/tickets/{ticketId:guid}/collect",
            async (Guid ticketId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CollectTicketCommand(ticketId), ct);
                return Results.Ok(result);
            }).RequireAuthorization();

        // UC-TK007: Makulera biljett
        app.MapDelete("/tickets/{ticketId:guid}",
            async (Guid ticketId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RevokeTicketCommand(ticketId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-PC004: Deaktivera kampanjkod
        app.MapPost("/promotion-codes/{promotionCodeId:guid}/deactivate",
            async (Guid promotionCodeId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivatePromotionCodeCommand(promotionCodeId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // UC-SA001: Skicka in staffansökan
        app.MapPost("/editions/{editionId:guid}/staff-applications",
            async (Guid editionId, SubmitStaffApplicationRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new SubmitStaffApplicationCommand(editionId, request.InterestDescription), ct);
                return Results.Created($"/staff-applications/{id}", new { id });
            }).RequireAuthorization();

        // UC-SA002: Lägg till tillgänglighet
        app.MapPost("/staff-applications/{applicationId:guid}/availabilities",
            async (Guid applicationId, AddAvailabilityRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new AddAvailabilityCommand(applicationId, request.From, request.To), ct);
                return Results.Created($"/staff-applications/{applicationId}/availabilities/{id}", new { id });
            }).RequireAuthorization();

        // UC-SA003: Ta bort tillgänglighet
        app.MapDelete("/staff-applications/{applicationId:guid}/availabilities/{availabilityId:guid}",
            async (Guid applicationId, Guid availabilityId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveAvailabilityCommand(applicationId, availabilityId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-SA004: Lägg till stationsönskemål
        app.MapPost("/staff-applications/{applicationId:guid}/station-preferences",
            async (Guid applicationId, StationPreferenceRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddStationPreferenceCommand(applicationId, request.StationId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-SA005: Ta bort stationsönskemål
        app.MapDelete("/staff-applications/{applicationId:guid}/station-preferences/{stationId:guid}",
            async (Guid applicationId, Guid stationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveStationPreferenceCommand(applicationId, stationId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-SA006: Acceptera staffansökan
        app.MapPost("/staff-applications/{applicationId:guid}/accept",
            async (Guid applicationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AcceptStaffApplicationCommand(applicationId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-SA007: Avslå staffansökan
        app.MapPost("/staff-applications/{applicationId:guid}/reject",
            async (Guid applicationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RejectStaffApplicationCommand(applicationId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // UC-SR001: Registrera för session
        app.MapPost("/sessions/{sessionId:guid}/registrations",
            async (Guid sessionId, RegisterForSessionRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new RegisterForSessionCommand(sessionId, request.TicketId), ct);
                return Results.Created($"/session-registrations/{id}", new { id });
            }).RequireAuthorization();

        // UC-SR002: Avboka sessionsregistrering
        app.MapDelete("/session-registrations/{registrationId:guid}",
            async (Guid registrationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelSessionRegistrationCommand(registrationId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // 3.2.10 – Lägg till bevakning
        app.MapPost("/sessions/{sessionId:guid}/watch",
            async (Guid sessionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new WatchSessionCommand(sessionId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // 3.2.10 – Ta bort bevakning
        app.MapDelete("/sessions/{sessionId:guid}/watch",
            async (Guid sessionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UnwatchSessionCommand(sessionId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        // Admin: lägg till bekräftad funktionär (find-or-create person)
        app.MapPost("/editions/{editionId:guid}/staff",
            async (Guid editionId, AddStaffMemberRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new AddStaffMemberCommand(editionId, request.Name, request.Email, request.Phone, request.Note), ct);
                return Results.Created($"/staff-applications/{id}", new { id });
            })
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // 3.2.4 – Min besökarregistrering
        app.MapGet("/editions/{editionId:guid}/my-visitor-registration",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyVisitorRegistrationQuery(editionId), ct)))
            .RequireAuthorization();

        // 3.2.5 – Valbara biljettyper för besökare
        app.MapGet("/editions/{editionId:guid}/available-ticket-types",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListAvailableTicketTypesQuery(editionId), ct)))
            .RequireAuthorization();

        // 3.2.4 – Mina sessionsregistreringar
        app.MapGet("/editions/{editionId:guid}/my-session-registrations",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMySessionRegistrationsQuery(editionId), ct)))
            .RequireAuthorization();

        // 3.2.11 – Mina arrangörssessioner
        app.MapGet("/editions/{editionId:guid}/my-organiser-sessions",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyOrganiserSessionsQuery(editionId), ct)))
            .RequireAuthorization();

        // 3.2.11 – Mina bemanningspass
        app.MapGet("/editions/{editionId:guid}/my-assigned-shifts",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyAssignedShiftsQuery(editionId), ct)))
            .RequireAuthorization();

        // 3.2.10 – Mina bevakade sessioner
        app.MapGet("/editions/{editionId:guid}/my-watched-sessions",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyWatchedSessionsQuery(editionId), ct)))
            .RequireAuthorization();

        // 3.2.4 – Min staffansökan
        app.MapGet("/editions/{editionId:guid}/my-staff-application",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyStaffApplicationQuery(editionId), ct)))
            .RequireAuthorization();

        // 3.1.8 – Lista biljettyper
        app.MapGet("/editions/{editionId:guid}/ticket-types",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListTicketTypesQuery(editionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // UC-PC002: Lista kampanjkoder för upplaga
        app.MapGet("/editions/{editionId:guid}/promotion-codes",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListPromotionCodesQuery(editionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // UC-PC005: Visa inlösningshistorik för kampanjkod
        app.MapGet("/promotion-codes/{promotionCodeId:guid}/redemptions",
            async (Guid promotionCodeId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListPromotionCodeRedemptionsQuery(promotionCodeId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // 3.1.8 – Uppdatera biljetttyp (UC-TK005)
        app.MapPut("/editions/{editionId:guid}/ticket-types/{ticketTypeId:guid}",
            async (Guid ticketTypeId, UpdateTicketTypeRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateTicketTypeCommand(ticketTypeId, request.Name, request.Price,
                    request.Category, request.ValidDays, request.AllowedCategories), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // 3.1.8 – Ta bort biljetttyp (UC-TK006)
        app.MapDelete("/editions/{editionId:guid}/ticket-types/{ticketTypeId:guid}",
            async (Guid ticketTypeId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeleteTicketTypeCommand(ticketTypeId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // 3.1.8 – Lista besökarregistreringar
        app.MapGet("/editions/{editionId:guid}/visitor-registrations",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListVisitorRegistrationsQuery(editionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        // UC-SA007: Lista staffansökningar per upplaga
        app.MapGet("/editions/{editionId:guid}/staff-applications",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListStaffApplicationsQuery(editionId), ct)))
            .RequireAuthorization(AuthConstants.Policies.IsAdmin);

        return app;
    }
}

public record CreateTicketTypeRequest(string Name, int Price, TicketTypeCategory Category, IReadOnlyList<DateOnly>? ValidDays = null, Guid[]? AllowedCategories = null);
public record UpdateTicketTypeRequest(string Name, int Price, TicketTypeCategory Category, IReadOnlyList<DateOnly>? ValidDays = null, Guid[]? AllowedCategories = null);
public record SubmitVisitorRegistrationRequest(Guid TicketTypeId);
public record ConfirmPaymentRequest(string ExternalReference);
public record CreatePromotionCodeRequest(
    string Code,
    string Description,
    PromotionDiscountType DiscountType,
    int DiscountValue,
    int? MaxRedemptions,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    Guid[]? AllowedTicketTypeIds);
public record ManualTicketPaymentRequest(string? ExternalReference);
public record TicketPaymentWebhookRequest(Guid VisitorRegistrationId, string ExternalReference, string PaymentStatus);
public record RedeemPromotionCodeRequest(string Code);
public record IssueTicketRequest(Guid PersonId, Guid TicketTypeId);
public record SubmitStaffApplicationRequest(string InterestDescription);
public record AddAvailabilityRequest(DateTime From, DateTime To);
public record StationPreferenceRequest(Guid StationId);
public record RegisterForSessionRequest(Guid TicketId);
public record AddStaffMemberRequest(string Name, string Email, string? Phone, string? Note);
