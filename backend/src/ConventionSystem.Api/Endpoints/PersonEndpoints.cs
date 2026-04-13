using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreatePerson;
using ConventionSystem.Application.Convention.Commands.DeactivatePerson;
using ConventionSystem.Application.Convention.Commands.ReactivatePerson;
using ConventionSystem.Application.Convention.Commands.UpdatePerson;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Api.Endpoints;

public static class PersonEndpoints
{
    public static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/conventions/{conventionId:guid}/persons",
            async (Guid conventionId, CreatePersonRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new CreatePersonCommand(conventionId, request.Name, request.Email, request.Phone), ct);
                return Results.Created($"/persons/{id}", new { id });
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPut("/persons/{personId:guid}",
            async (Guid personId, UpdatePersonRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(
                    new UpdatePersonCommand(personId, request.Name, request.Email, request.Phone), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapDelete("/persons/{personId:guid}",
            async (Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivatePersonCommand(personId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/persons/{personId:guid}/reactivate",
            async (Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ReactivatePersonCommand(personId), ct);
                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/persons/{personId:guid}/send-reset-link",
            async (Guid personId,
                UserManager<ApplicationUser> userManager,
                IPersonRepository personRepo,
                IEmailService emailService,
                CancellationToken ct) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.PersonId == personId, ct);
                if (user is null)
                    return Results.NotFound();

                var person = await personRepo.GetByIdAsync(new PersonId(personId), ct);
                if (person is null)
                    return Results.NotFound();

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = $"?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email ?? person.Email)}";
                await emailService.SendPasswordResetAsync(person.Email, person.Name, resetLink, ct);

                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/persons/{personId:guid}/lock",
            async (Guid personId, UserManager<ApplicationUser> userManager, CancellationToken ct) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.PersonId == personId, ct);
                if (user is null)
                    return Results.NotFound();

                await userManager.SetLockoutEnabledAsync(user, true);
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        app.MapPost("/persons/{personId:guid}/unlock",
            async (Guid personId, UserManager<ApplicationUser> userManager, CancellationToken ct) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.PersonId == personId, ct);
                if (user is null)
                    return Results.NotFound();

                await userManager.SetLockoutEndDateAsync(user, null);

                return Results.NoContent();
            }).RequireAuthorization(AuthConstants.Policies.IsAdmin);

        return app;
    }
}

public record CreatePersonRequest(string Name, string Email, string? Phone);
public record UpdatePersonRequest(string Name, string Email, string? Phone);
