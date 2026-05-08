using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Commands.CancelTeamRegistration;
using ConventionSystem.Application.Registration.Commands.ConfirmTeamRegistration;
using ConventionSystem.Application.Registration.Commands.RegisterTeamForEvent;
using ConventionSystem.Application.Registration.Queries.GetTeamRegistration;
using ConventionSystem.Application.Registration.Queries.ListTeamRegistrations;
using ConventionSystem.Application.Event.Commands.ConfigureTeamRegistration;

namespace ConventionSystem.Api.Endpoints;

public static class TeamRegistrationEndpoints
{
    public static void MapTeamRegistrationEndpoints(this RouteGroups groups)
    {
        groups.Admin.MapPut("/api/events/{eventId:guid}/registration-mode",
            async (Guid eventId, ConfigureTeamRegistrationRequest req, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ConfigureTeamRegistrationCommand(
                    eventId, req.RegistrationMode, req.MinTeamSize, req.MaxTeamSize), ct);
                return Results.NoContent();
            });

        groups.Admin.MapGet("/api/events/{eventId:guid}/team-registrations",
            async (Guid eventId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListTeamRegistrationsQuery(eventId), ct)));

        groups.Authenticated.MapPost("/api/events/{eventId:guid}/team-registrations",
            async (Guid eventId, RegisterTeamRequest req, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new RegisterTeamForEventCommand(eventId, req.EditionId, req.TeamName), ct);
                return Results.Created($"/api/team-registrations/{id}", new { id });
            });

        groups.Authenticated.MapGet("/api/team-registrations/{id:guid}",
            async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var dto = await sender.Send(new GetTeamRegistrationQuery(id), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

        groups.Authenticated.MapPost("/api/team-registrations/{id:guid}/confirm",
            async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ConfirmTeamRegistrationCommand(id), ct);
                return Results.NoContent();
            });

        groups.Authenticated.MapPost("/api/team-registrations/{id:guid}/cancel",
            async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelTeamRegistrationCommand(id), ct);
                return Results.NoContent();
            });
    }
}

public sealed record ConfigureTeamRegistrationRequest(string RegistrationMode, int? MinTeamSize, int? MaxTeamSize);
public sealed record RegisterTeamRequest(Guid EditionId, string TeamName);
