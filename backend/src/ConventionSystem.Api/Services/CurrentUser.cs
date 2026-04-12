using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Common;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public PersonId PersonId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(AuthConstants.Claims.PersonId)
                ?? throw new UnauthorizedAccessException("Ingen inloggad användare hittades.");

            if (!Guid.TryParse(claim.Value, out var guid))
                throw new UnauthorizedAccessException("Ogiltigt person_id i token.");

            return new PersonId(guid);
        }
    }
}
