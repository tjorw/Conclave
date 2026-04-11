using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
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
            ITenantContext tenantContext,
            IConventionRepository conventionRepo,
            IPersonRepository personRepo,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!tenantContext.IsResolved)
                return Results.BadRequest("X-Convention-Id-header saknas eller är ogiltig.");

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            var conventionId = new ConventionId(tenantContext.ConventionId);

            // UC002: identifiera eller skapa person
            var link = await identityDb.ConventionUserLinks
                .FirstOrDefaultAsync(l => l.UserId == user.Id && l.ConventionId == tenantContext.ConventionId, ct);

            Guid personId;
            if (link is not null)
            {
                // Befintlig person – återinloggning
                personId = link.PersonId;
            }
            else
            {
                // Första inloggningen till denna konvention – identifiera eller skapa person
                var existingPerson = await personRepo.FindByEmailInConventionAsync(conventionId, request.Email, ct);

                if (existingPerson is not null)
                {
                    // Person finns redan (t.ex. skapad av admin) – koppla identitetskontot
                    personId = existingPerson.Id.Value;
                }
                else
                {
                    // Skapa nytt personkonto; namn samlas in i registreringsflödet (UC-VR001/SA001/EV001)
                    var convention = await conventionRepo.GetByIdAsync(conventionId, ct);
                    if (convention is null)
                        return Results.BadRequest("Konventionen hittades inte.");

                    var person = convention.RegisterPerson(string.Empty, request.Email);
                    await personRepo.AddAndSaveAsync(person, ct);
                    personId = person.Id.Value;
                }

                var newLink = ConventionUserLink.Create(user.Id, tenantContext.ConventionId, personId);
                await identityDb.ConventionUserLinks.AddAsync(newLink, ct);
                await identityDb.SaveChangesAsync(ct);
            }

            var token = IssueJwt(personId, configuration);
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

public record LoginRequest(string Email, string Password);
