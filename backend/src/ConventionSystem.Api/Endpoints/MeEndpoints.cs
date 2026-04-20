using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.UpdatePerson;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this RouteGroups groups)
    {
        groups.Authenticated.MapGet("/me/profile", async (
            ICurrentUser currentUser,
            IPersonRepository personRepo,
            CancellationToken ct) =>
        {
            var person = await personRepo.GetByIdAsync(currentUser.PersonId, ct);
            if (person is null)
                return Results.NotFound();

            return Results.Ok(new MyProfileDto(person.Name, person.Email, person.Phone));
        });

        groups.Authenticated.MapPut("/me/profile", async (
            UpdateProfileRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new UpdatePersonCommand(
                currentUser.PersonId.Value,
                request.Name,
                request.Email,
                request.Phone), ct);

            return Results.NoContent();
        });
    }
}

public record MyProfileDto(string Name, string Email, string? Phone);
public record UpdateProfileRequest(string Name, string Email, string? Phone);
