using ConventionSystem.Api.Auth;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
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
            IConventionRepository conventionRepo,
            IPersonRepository personRepo,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            var convention = await conventionRepo.GetSingleAsync(ct);
            if (convention is null)
                return Results.Problem("Konventionen är inte konfigurerad.");

            var conventionId = convention.Id;

            Guid personId;
            if (user.PersonId.HasValue)
            {
                // Återinloggning – PersonId redan känt
                personId = user.PersonId.Value;
            }
            else
            {
                // Första inloggningen – identifiera eller skapa person
                var existingPerson = await personRepo.FindByEmailInConventionAsync(conventionId, request.Email, ct);

                if (existingPerson is not null)
                {
                    // Person finns redan (t.ex. skapad av admin) – koppla identitetskontot
                    personId = existingPerson.Id.Value;
                }
                else
                {
                    // Skapa nytt personkonto; namn samlas in i registreringsflödet
                    var person = convention.RegisterPerson(string.Empty, request.Email);
                    await personRepo.AddAndSaveAsync(person, ct);
                    personId = person.Id.Value;
                }

                user.PersonId = personId;
                await userManager.UpdateAsync(user);
            }

            var isAdmin = convention.IsAdministrator(new PersonId(personId));
            var token = IssueJwt(personId, isAdmin, configuration);
            return Results.Ok(new { token });
        });

        return app;
    }

    private static string IssueJwt(Guid personId, bool isAdmin, IConfiguration configuration)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        List<Claim> claims = [new Claim(AuthConstants.Claims.PersonId, personId.ToString())];
        if (isAdmin)
            claims.Add(new Claim(AuthConstants.Claims.IsAdmin, "true"));

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

public record LoginRequest(string Email, string Password);
