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

    public bool IsAdmin
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(AuthConstants.Claims.IsAdmin);
            return claim is not null && bool.TryParse(claim.Value, out var result) && result;
        }
    }

    public bool IsReception
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(AuthConstants.Claims.IsReception);
            return claim is not null && bool.TryParse(claim.Value, out var result) && result;
        }
    }
}
