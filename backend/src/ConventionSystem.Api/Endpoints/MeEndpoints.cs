using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Commands.UpdatePerson;
using MediatR;

namespace ConventionSystem.Api.Endpoints;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/me/profile", async (
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
        }).RequireAuthorization();

        return app;
    }
}

public record UpdateProfileRequest(string Name, string Email, string? Phone);
