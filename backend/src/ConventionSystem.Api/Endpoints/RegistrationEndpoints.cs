using ConventionSystem.Application.Registration.Commands.AcceptStaffApplication;
using ConventionSystem.Application.Registration.Commands.AddAvailability;
using ConventionSystem.Application.Registration.Commands.AddStaffMember;
using ConventionSystem.Application.Registration.Commands.AddStaffAreaPreference;
using ConventionSystem.Application.Registration.Commands.AssignOrganiserTicket;
using ConventionSystem.Application.Registration.Commands.AssignStaffTicket;
using ConventionSystem.Application.Registration.Commands.CancelSessionRegistration;
using ConventionSystem.Application.Registration.Commands.CancelOwnTicket;
using ConventionSystem.Application.Registration.Commands.CancelVisitorRegistration;
using ConventionSystem.Application.Registration.Commands.CollectTicket;
using ConventionSystem.Application.Registration.Commands.ConfirmTicketPaymentWebhook;
using ConventionSystem.Application.Registration.Commands.ConfirmVisitorRegistrationPayment;
using ConventionSystem.Application.Registration.Commands.CreatePromotionCode;
using ConventionSystem.Application.Registration.Commands.CreateTicketType;
using ConventionSystem.Application.Registration.Commands.DeactivatePromotionCode;
using ConventionSystem.Application.Registration.Commands.DeleteStaffApplication;
using ConventionSystem.Application.Registration.Commands.DeleteTicketType;
using ConventionSystem.Application.Registration.Commands.IssueTicket;
using ConventionSystem.Application.Registration.Commands.RedeemPromotionCode;
using ConventionSystem.Application.Registration.Commands.RegisterForSession;
using ConventionSystem.Application.Registration.Commands.RegisterManualTicketPayment;
using ConventionSystem.Application.Registration.Commands.RejectStaffApplication;
using ConventionSystem.Application.Registration.Commands.RemoveAvailability;
using ConventionSystem.Application.Registration.Commands.RemoveStaffAreaPreference;
using ConventionSystem.Application.Registration.Commands.RevokeTicket;
using ConventionSystem.Application.Registration.Commands.SubmitStaffApplication;
using ConventionSystem.Application.Registration.Commands.SubmitVisitorRegistration;
using ConventionSystem.Application.Registration.Commands.UnwatchSession;
using ConventionSystem.Application.Registration.Commands.UpdateStaffApplication;
using ConventionSystem.Application.Registration.Commands.WalkupRegister;
using ConventionSystem.Application.Registration.Commands.CreateWalkupPerson;
using ConventionSystem.Application.Registration.Queries.ListVisitorTicketTypesForWalkup;
using ConventionSystem.Application.Registration.Commands.UpdateTicketType;
using ConventionSystem.Application.Registration.Commands.WatchSession;
using ConventionSystem.Application.Registration.Queries.GetMySessionRegistrations;
using ConventionSystem.Application.Registration.Queries.GetMyStaffApplication;
using ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;
using ConventionSystem.Application.Registration.Queries.GetMyAssignedShifts;
using ConventionSystem.Application.Registration.Queries.GetMyOrganiserSessions;
using ConventionSystem.Application.Registration.Queries.GetMyWatchedSessions;
using ConventionSystem.Application.Registration.Queries.GetEventOrganiserTicketAssignments;
using ConventionSystem.Application.Registration.Queries.ListAvailableTicketTypes;
using ConventionSystem.Application.Registration.Queries.ListEditionOrganiserTicketAssignments;
using ConventionSystem.Application.Registration.Queries.ListEditionStaffTicketAssignments;
using ConventionSystem.Application.Registration.Queries.ListOrganiserTicketTypes;
using ConventionSystem.Application.Registration.Queries.ListStaffTicketTypes;
using ConventionSystem.Application.Registration.Queries.ListPromotionCodeRedemptions;
using ConventionSystem.Application.Registration.Queries.ListPromotionCodes;
using ConventionSystem.Application.Registration.Queries.ListTicketTypes;
using ConventionSystem.Application.Registration.Queries.ListVisitorRegistrations;
using ConventionSystem.Application.Staff.Queries.GetStaffApplication;
using ConventionSystem.Application.Staff.Queries.ListStaffApplications;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Application.Common;

namespace ConventionSystem.Api.Endpoints;

public static class RegistrationEndpoints
{
    public static void MapRegistrationEndpoints(this RouteGroups groups)
    {
        // --- Anonyma ---

        // UC-TK005: Bekräfta betalning via webhook
        groups.Anonymous.MapPost("/payments/webhook/tickets",
            async (TicketPaymentWebhookRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ConfirmTicketPaymentWebhookCommand(
                    request.VisitorRegistrationId,
                    request.ExternalReference,
                    request.PaymentStatus), ct);
                return Results.NoContent();
            });

        // --- Inloggade ---

        // UC-TK001: Skapa biljetttyp
        groups.Authenticated.MapPost("/editions/{editionId:guid}/ticket-types",
            async (Guid editionId, CreateTicketTypeRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateTicketTypeCommand(editionId, request.Name, request.Price, request.Category,
                    request.ValidDays, request.AllowedCategories, request.Description), ct);
                return Results.Created($"/ticket-types/{id}", new { id });
            });

        // UC-VR001: Anmäl som besökare
        groups.Authenticated.MapPost("/editions/{editionId:guid}/visitor-registrations",
            async (Guid editionId, SubmitVisitorRegistrationRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new SubmitVisitorRegistrationCommand(editionId, request.TicketTypeId), ct);
                return Results.Created($"/visitor-registrations/{id}", new { id });
            });

        // UC-PC003: Lös in kampanjkod
        groups.Authenticated.MapPost("/tickets/{ticketId:guid}/redeem-promotion-code",
            async (Guid ticketId, RedeemPromotionCodeRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RedeemPromotionCodeCommand(ticketId, request.Code), ct);
                return Results.Ok(result);
            });

        // UC-TK006: Avboka biljett (innehavare)
        groups.Authenticated.MapDelete("/my/tickets/{ticketId:guid}",
            async (Guid ticketId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelOwnTicketCommand(ticketId), ct);
                return Results.NoContent();
            });

        // UC-VR003: Avboka registrering
        groups.Authenticated.MapDelete("/visitor-registrations/{registrationId:guid}",
            async (Guid registrationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelVisitorRegistrationCommand(registrationId), ct);
                return Results.NoContent();
            });

        // UC-TK002: Utfärda biljett manuellt
        groups.Authenticated.MapPost("/editions/{editionId:guid}/tickets",
            async (Guid editionId, IssueTicketRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new IssueTicketCommand(request.PersonId, editionId, request.TicketTypeId), ct);
                return Results.Created($"/tickets/{id}", new { id });
            });

        // UC-TK008: Hämta ut biljett
        groups.Authenticated.MapPost("/tickets/{ticketId:guid}/collect",
            async (Guid ticketId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CollectTicketCommand(ticketId), ct);
                return Results.Ok(result);
            });

        // UC-RX005: Walk-up – skapa person
        groups.Authenticated.MapPost("/editions/{editionId:guid}/walkup-persons",
            async (Guid editionId, WalkupPersonRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new CreateWalkupPersonCommand(editionId, request.Name, request.Email, request.Phone), ct);
                return Results.Created($"/persons/{id}", new { id });
            });

        // UC-RX005: Walk-up – registrera och betala
        groups.Authenticated.MapPost("/editions/{editionId:guid}/walkup-registrations",
            async (Guid editionId, WalkupRegistrationRequest request, ISender sender, CancellationToken ct) =>
            {
                var ticketId = await sender.Send(
                    new WalkupRegisterCommand(editionId, request.PersonId, request.TicketTypeId), ct);
                return Results.Created($"/tickets/{ticketId}", new { ticketId });
            });

        // UC-RX005: Walk-up – biljetttyper för reception
        groups.Authenticated.MapGet("/editions/{editionId:guid}/walkup-ticket-types",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListVisitorTicketTypesForWalkupQuery(editionId), ct)));

        // UC-TK007: Makulera biljett
        groups.Authenticated.MapDelete("/tickets/{ticketId:guid}",
            async (Guid ticketId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RevokeTicketCommand(ticketId), ct);
                return Results.NoContent();
            });

        // UC-SA001: Skicka in staffansökan
        groups.Authenticated.MapPost("/editions/{editionId:guid}/staff-applications",
            async (Guid editionId, SubmitStaffApplicationRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new SubmitStaffApplicationCommand(editionId, request.InterestDescription), ct);
                return Results.Created($"/staff-applications/{id}", new { id });
            });

        var staffApps = groups.Authenticated.MapGroup("/staff-applications/{applicationId:guid}");

        // UC-SA002: Lägg till tillgänglighet
        staffApps.MapPost("/availabilities",
            async (Guid applicationId, AddAvailabilityRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new AddAvailabilityCommand(applicationId, request.From, request.To), ct);
                return Results.Created($"/staff-applications/{applicationId}/availabilities/{id}", new { id });
            });

        // UC-SA003: Ta bort tillgänglighet
        staffApps.MapDelete("/availabilities/{availabilityId:guid}",
            async (Guid applicationId, Guid availabilityId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveAvailabilityCommand(applicationId, availabilityId), ct);
                return Results.NoContent();
            });

        // UC-SA004: Lägg till stationsönskemål
        staffApps.MapPost("/staff-area-preferences",
            async (Guid applicationId, StaffAreaPreferenceRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddStaffAreaPreferenceCommand(applicationId, request.StaffAreaId), ct);
                return Results.NoContent();
            });

        // UC-SA005: Ta bort stationsönskemål
        staffApps.MapDelete("/staff-area-preferences/{staffAreaId:guid}",
            async (Guid applicationId, Guid staffAreaId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveStaffAreaPreferenceCommand(applicationId, staffAreaId), ct);
                return Results.NoContent();
            });

        // UC-SA006: Acceptera staffansökan
        staffApps.MapPost("/accept",
            async (Guid applicationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AcceptStaffApplicationCommand(applicationId), ct);
                return Results.NoContent();
            });

        // UC-SA007: Avslå staffansökan
        staffApps.MapPost("/reject",
            async (Guid applicationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RejectStaffApplicationCommand(applicationId), ct);
                return Results.NoContent();
            });

        // UC-SR001: Registrera för session
        groups.Authenticated.MapPost("/sessions/{sessionId:guid}/registrations",
            async (Guid sessionId, RegisterForSessionRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new RegisterForSessionCommand(sessionId, request.TicketId), ct);
                return Results.Created($"/session-registrations/{id}", new { id });
            });

        // UC-SR002: Avboka sessionsregistrering
        groups.Authenticated.MapDelete("/session-registrations/{registrationId:guid}",
            async (Guid registrationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelSessionRegistrationCommand(registrationId), ct);
                return Results.NoContent();
            });

        // 3.2.10 – Lägg till / ta bort bevakning
        groups.Authenticated.MapPost("/sessions/{sessionId:guid}/watch",
            async (Guid sessionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new WatchSessionCommand(sessionId), ct);
                return Results.NoContent();
            });

        groups.Authenticated.MapDelete("/sessions/{sessionId:guid}/watch",
            async (Guid sessionId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UnwatchSessionCommand(sessionId), ct);
                return Results.NoContent();
            });

        // 3.2.4 – Min besökarregistrering
        groups.Authenticated.MapGet("/editions/{editionId:guid}/my-visitor-registration",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyVisitorRegistrationQuery(editionId), ct)));

        // 3.2.5 – Valbara biljettyper för besökare
        groups.Authenticated.MapGet("/editions/{editionId:guid}/available-ticket-types",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListAvailableTicketTypesQuery(editionId), ct)));

        // UC-EV013: Informativa arrangörsbiljetter vid arrangemangsanmälan
        groups.Authenticated.MapGet("/editions/{editionId:guid}/organiser-ticket-types",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListOrganiserTicketTypesQuery(editionId), ct)));

        // UC-EV014: Nuvarande arrangörsbiljetter för publiceringsvyn
        groups.Authenticated.MapGet("/events/{eventId:guid}/organiser-ticket-assignments",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetEventOrganiserTicketAssignmentsQuery(eventId), ct)));

        // 3.2.4 – Mina sessionsregistreringar
        groups.Authenticated.MapGet("/editions/{editionId:guid}/my-session-registrations",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMySessionRegistrationsQuery(editionId), ct)));

        // 3.2.11 – Mina arrangörssessioner
        groups.Authenticated.MapGet("/editions/{editionId:guid}/my-organiser-sessions",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyOrganiserSessionsQuery(editionId), ct)));

        // 3.2.11 – Mina bemanningspass
        groups.Authenticated.MapGet("/editions/{editionId:guid}/my-assigned-shifts",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyAssignedShiftsQuery(editionId), ct)));

        // 3.2.10 – Mina bevakade sessioner
        groups.Authenticated.MapGet("/editions/{editionId:guid}/my-watched-sessions",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyWatchedSessionsQuery(editionId), ct)));

        // 3.2.4 – Min staffansökan
        groups.Authenticated.MapGet("/editions/{editionId:guid}/my-staff-application",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyStaffApplicationQuery(editionId), ct)));

        // --- Admin ---

        // UC-PC001: Skapa kampanjkod
        groups.Admin.MapPost("/editions/{editionId:guid}/promotion-codes",
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
            });

        // UC-VR002: Bekräfta betalning
        groups.Admin.MapPost("/visitor-registrations/{registrationId:guid}/confirm-payment",
            async (Guid registrationId, ConfirmPaymentRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ConfirmVisitorRegistrationPaymentCommand(registrationId, request.ExternalReference), ct);
                return Results.NoContent();
            });

        // UC-TK004: Registrera manuell betalning
        groups.Admin.MapPost("/tickets/{ticketId:guid}/manual-payment",
            async (Guid ticketId, ManualTicketPaymentRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RegisterManualTicketPaymentCommand(ticketId, request.ExternalReference), ct);
                return Results.NoContent();
            });

        // UC-PC004: Deaktivera kampanjkod
        groups.Admin.MapPost("/promotion-codes/{promotionCodeId:guid}/deactivate",
            async (Guid promotionCodeId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivatePromotionCodeCommand(promotionCodeId), ct);
                return Results.NoContent();
            });

        // Admin: lägg till bekräftad funktionär (find-or-create person)
        groups.Admin.MapPost("/editions/{editionId:guid}/staff",
            async (Guid editionId, AddStaffMemberRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new AddStaffMemberCommand(editionId, request.Name, request.Email, request.Phone, request.Note), ct);
                return Results.Created($"/staff-applications/{id}", new { id });
            });

        // UC-EV015: Manuell hantering av arrangörsbiljetter
        groups.Admin.MapGet("/editions/{editionId:guid}/organiser-ticket-assignments",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionOrganiserTicketAssignmentsQuery(editionId), ct)));

        groups.Admin.MapPut("/editions/{editionId:guid}/organiser-ticket-assignments/{personId:guid}",
            async (Guid editionId, Guid personId, AssignOrganiserTicketRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AssignOrganiserTicketCommand(editionId, personId, request.TicketTypeId), ct);
                return Results.NoContent();
            });

        // R-ST01: Funktionärsbiljetter – lista biljetttyper och tilldelning
        groups.Admin.MapGet("/editions/{editionId:guid}/staff-ticket-types",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListStaffTicketTypesQuery(editionId), ct)));

        groups.Admin.MapGet("/editions/{editionId:guid}/staff-ticket-assignments",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListEditionStaffTicketAssignmentsQuery(editionId), ct)));

        groups.Admin.MapPut("/editions/{editionId:guid}/staff-ticket-assignments/{personId:guid}",
            async (Guid editionId, Guid personId, AssignStaffTicketRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AssignStaffTicketCommand(editionId, personId, request.TicketTypeId), ct);
                return Results.NoContent();
            });

        // 3.1.8 – Lista biljettyper
        groups.Admin.MapGet("/editions/{editionId:guid}/ticket-types",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListTicketTypesQuery(editionId), ct)));

        // UC-PC002: Lista kampanjkoder för upplaga
        groups.Admin.MapGet("/editions/{editionId:guid}/promotion-codes",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListPromotionCodesQuery(editionId), ct)));

        // UC-PC005: Visa inlösningshistorik för kampanjkod
        groups.Admin.MapGet("/promotion-codes/{promotionCodeId:guid}/redemptions",
            async (Guid promotionCodeId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListPromotionCodeRedemptionsQuery(promotionCodeId), ct)));

        // 3.1.8 – Uppdatera biljetttyp
        groups.Admin.MapPut("/editions/{editionId:guid}/ticket-types/{ticketTypeId:guid}",
            async (Guid ticketTypeId, UpdateTicketTypeRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateTicketTypeCommand(ticketTypeId, request.Name, request.Price,
                    request.Category, request.ValidDays, request.AllowedCategories, request.Description), ct);
                return Results.NoContent();
            });

        // 3.1.8 – Ta bort biljetttyp
        groups.Admin.MapDelete("/editions/{editionId:guid}/ticket-types/{ticketTypeId:guid}",
            async (Guid ticketTypeId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeleteTicketTypeCommand(ticketTypeId), ct);
                return Results.NoContent();
            });

        // 3.1.8 – Lista besökarregistreringar
        groups.Admin.MapGet("/editions/{editionId:guid}/visitor-registrations",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListVisitorRegistrationsQuery(editionId), ct)));

        // UC-SA007: Lista staffansökningar per upplaga
        groups.Admin.MapGet("/editions/{editionId:guid}/staff-applications",
            async (Guid editionId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListStaffApplicationsQuery(editionId), ct)));

        groups.Admin.MapGet("/staff-applications/{applicationId:guid}",
            async (Guid applicationId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStaffApplicationQuery(applicationId), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });

        groups.Admin.MapPut("/staff-applications/{applicationId:guid}",
            async (Guid applicationId, UpdateStaffApplicationRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateStaffApplicationCommand(
                    applicationId,
                    request.InterestDescription,
                    request.Availabilities.Select(a => new UpdateStaffApplicationAvailability(a.From, a.To)).ToList(),
                    request.StaffAreaIds), ct);
                return Results.NoContent();
            });

        groups.Admin.MapDelete("/staff-applications/{applicationId:guid}",
            async (Guid applicationId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeleteStaffApplicationCommand(applicationId), ct);
                return Results.NoContent();
            });
    }
}

public record CreateTicketTypeRequest(string Name, int Price, TicketTypeCategory Category, IReadOnlyList<DateOnly>? ValidDays = null, Guid[]? AllowedCategories = null, string? Description = null);
public record UpdateTicketTypeRequest(string Name, int Price, TicketTypeCategory Category, IReadOnlyList<DateOnly>? ValidDays = null, Guid[]? AllowedCategories = null, string? Description = null);
public record AssignOrganiserTicketRequest(Guid? TicketTypeId);
public record AssignStaffTicketRequest(Guid? TicketTypeId);
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
public record WalkupPersonRequest(string Name, string Email, string? Phone);
public record WalkupRegistrationRequest(Guid PersonId, Guid TicketTypeId);
public record AddAvailabilityRequest(DateTime From, DateTime To);
public record StaffAreaPreferenceRequest(Guid StaffAreaId);
public record RegisterForSessionRequest(Guid TicketId);
public record AddStaffMemberRequest(string Name, string Email, string? Phone, string? Note);
public record UpdateStaffApplicationRequest(
    string InterestDescription,
    IReadOnlyList<UpdateStaffApplicationAvailabilityRequest> Availabilities,
    IReadOnlyList<Guid> StaffAreaIds);
public record UpdateStaffApplicationAvailabilityRequest(DateTime From, DateTime To);
