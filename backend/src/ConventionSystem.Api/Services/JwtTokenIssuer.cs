using ConventionSystem.Api.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ConventionSystem.Api.Services;

public sealed class JwtTokenIssuer(IOptions<JwtOptions> options) : IJwtTokenIssuer
{
    private readonly JwtOptions _options = options.Value;

    public string Issue(
        Guid? personId,
        bool isAdmin,
        bool isReception,
        bool isSystemAdmin,
        string userType,
        Guid? tenantId)
    {
        List<Claim> claims = [new(AuthConstants.Claims.UserType, userType)];

        if (personId.HasValue)
            claims.Add(new Claim(AuthConstants.Claims.PersonId, personId.Value.ToString()));
        if (tenantId.HasValue)
            claims.Add(new Claim(AuthConstants.Claims.TenantId, tenantId.Value.ToString()));
        if (isAdmin)
            claims.Add(new Claim(AuthConstants.Claims.IsAdmin, AuthConstants.Claims.IsAdminTrue));
        if (isReception)
            claims.Add(new Claim(AuthConstants.Claims.IsReception, AuthConstants.Claims.IsReceptionTrue));
        if (isSystemAdmin)
            claims.Add(new Claim(AuthConstants.Claims.IsSystemAdmin, AuthConstants.Claims.IsSystemAdminTrue));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTimeOffset.UtcNow.AddHours(8).UtcDateTime,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
