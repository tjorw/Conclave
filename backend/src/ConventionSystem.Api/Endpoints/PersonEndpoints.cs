using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreatePerson;
using ConventionSystem.Application.Convention.Commands.DeactivatePerson;
using ConventionSystem.Application.Convention.Commands.ReactivatePerson;
using ConventionSystem.Application.Convention.Commands.UpdatePerson;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Api.Endpoints;

public static class PersonEndpoints
{
    public static void MapPersonEndpoints(this RouteGroups groups)
    {
        groups.Admin.MapPost("/conventions/{conventionId:guid}/persons",
            async (Guid conventionId, CreatePersonRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(
                    new CreatePersonCommand(conventionId, request.Name, request.Email, request.Phone), ct);
                return Results.Created($"/persons/{id}", new { id });
            });

        var persons = groups.Admin.MapGroup("/persons/{personId:guid}");

        persons.MapPut("/",
            async (Guid personId, UpdatePersonRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(
                    new UpdatePersonCommand(personId, request.Name, request.Email, request.Phone), ct);
                return Results.NoContent();
            });

        persons.MapDelete("/",
            async (Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivatePersonCommand(personId), ct);
                return Results.NoContent();
            });

        persons.MapPost("/reactivate",
            async (Guid personId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ReactivatePersonCommand(personId), ct);
                return Results.NoContent();
            });

        persons.MapPost("/send-reset-link",
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
            });

        persons.MapPost("/lock",
            async (Guid personId, UserManager<ApplicationUser> userManager, CancellationToken ct) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.PersonId == personId, ct);
                if (user is null)
                    return Results.NotFound();

                await userManager.SetLockoutEnabledAsync(user, true);
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

                return Results.NoContent();
            });

        persons.MapPost("/unlock",
            async (Guid personId, UserManager<ApplicationUser> userManager, CancellationToken ct) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.PersonId == personId, ct);
                if (user is null)
                    return Results.NotFound();

                await userManager.SetLockoutEndDateAsync(user, null);

                return Results.NoContent();
            });
    }
}

public record CreatePersonRequest(string Name, string Email, string? Phone);
public record UpdatePersonRequest(string Name, string Email, string? Phone);
