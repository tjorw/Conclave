using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Infrastructure.System;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ConventionSystem.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        // OBS: detta endpoint är oskyddat och bör skyddas med ett API-nyckel eller admin-roll
        // innan systemet exponeras publikt.
        app.MapPost("/system/conventions", async (
            ProvisionConventionRequest request,
            TenantContext tenantContext,
            SystemDbContext systemDb,
            UserManager<ApplicationUser> userManager,
            ApplicationIdentityDbContext identityDb,
            ISender sender,
            IServiceProvider services,
            CancellationToken ct) =>
        {
            var conventionId = Guid.CreateVersion7();

            // Löser tenant-kontexten med den angivna connection string så att
            // ConventionDbContext byggs mot rätt databas när handlern körs.
            tenantContext.Resolve(conventionId, request.ConnectionString);

            // Skapa konventionen och registrantpersonen i ConventionDb
            await sender.Send(new CreateConventionCommand(
                request.Name,
                request.Slug,
                request.RegistrantName,
                request.RegistrantEmail,
                conventionId), ct);

            // Slå upp registrantens PersonId (skapad av CreateConventionHandler)
            // Löses lazily efter att tenant-kontexten är klar.
            var personRepo = services.GetRequiredService<IPersonRepository>();
            var registrant = await personRepo.FindByEmailInConventionAsync(
                new ConventionId(conventionId), request.RegistrantEmail, ct);
            if (registrant is null)
                return Results.Problem("Kunde inte hitta den skapade registranten.");

            // Skapa tenant-post i SystemDb
            var tenant = Tenant.Create(conventionId, request.Slug, request.ConnectionString, request.Domain);
            await systemDb.Tenants.AddAsync(tenant, ct);
            await systemDb.SaveChangesAsync(ct);

            // Skapa identity-konto för registranten
            var user = new ApplicationUser
            {
                UserName = request.RegistrantEmail,
                Email = request.RegistrantEmail
            };
            var identityResult = await userManager.CreateAsync(user, request.RegistrantPassword);
            if (!identityResult.Succeeded)
                return Results.ValidationProblem(
                    identityResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

            // Koppla identity-konto till personens PersonId i konventionen
            var link = ConventionUserLink.Create(user.Id, conventionId, registrant.Id.Value);
            await identityDb.ConventionUserLinks.AddAsync(link, ct);
            await identityDb.SaveChangesAsync(ct);

            return Results.Created($"/system/conventions/{conventionId}", new { conventionId });
        });

        return app;
    }
}

public record ProvisionConventionRequest(
    string Name,
    string Slug,
    string RegistrantName,
    string RegistrantEmail,
    string RegistrantPassword,
    string ConnectionString,
    string? Domain = null);
