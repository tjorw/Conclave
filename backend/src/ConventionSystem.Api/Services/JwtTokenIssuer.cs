using ConventionSystem.Api.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ConventionSystem.Api.Services;

public sealed class JwtTokenIssuer(IConfiguration configuration) : IJwtTokenIssuer
{
    public string Issue(
        Guid? personId,
        bool isAdmin,
        bool isSystemAdmin,
        string userType,
        Guid? tenantId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        List<Claim> claims = [new(AuthConstants.Claims.UserType, userType)];

        if (personId.HasValue)
            claims.Add(new Claim(AuthConstants.Claims.PersonId, personId.Value.ToString()));
        if (tenantId.HasValue)
            claims.Add(new Claim(AuthConstants.Claims.TenantId, tenantId.Value.ToString()));
        if (isAdmin)
            claims.Add(new Claim(AuthConstants.Claims.IsAdmin, AuthConstants.Claims.IsAdminTrue));
        if (isSystemAdmin)
            claims.Add(new Claim(AuthConstants.Claims.IsSystemAdmin, AuthConstants.Claims.IsSystemAdminTrue));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTimeOffset.UtcNow.AddHours(8).UtcDateTime,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
