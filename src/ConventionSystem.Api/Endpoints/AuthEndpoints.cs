using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ConventionSystem.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            ApplicationIdentityDbContext identityDb,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            var link = await identityDb.ConventionUserLinks
                .FirstOrDefaultAsync(
                    l => l.UserId == user.Id && l.ConventionId == request.ConventionId, ct);

            if (link is null)
                return Results.Forbid();

            var token = IssueJwt(link.PersonId, configuration);
            return Results.Ok(new { token });
        });

        return app;
    }

    private static string IssueJwt(Guid personId, IConfiguration configuration)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("person_id", personId.ToString())]),
            Expires = DateTimeOffset.UtcNow.AddHours(8).UtcDateTime,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}

public record LoginRequest(string Email, string Password, Guid ConventionId);
