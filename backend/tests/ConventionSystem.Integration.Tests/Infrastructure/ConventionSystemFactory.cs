using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace ConventionSystem.Integration.Tests.Infrastructure;

public sealed class ConventionSystemFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private static int _userCounter;

    public const string AdminEmail = "admin@test.se";
    public const string AdminPassword = "Admin123!";

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        // Trigga uppstart – kör migrationer för ConventionDbContext och ApplicationIdentityDbContext
        _ = Server;

        await SetupConventionAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sql.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Skapar ett nytt identity-konto utan PersonId. PersonId sätts vid första inloggningen.
    /// </summary>
    public async Task<(string Email, string Password)> CreateTestUserAsync()
    {
        var suffix = Interlocked.Increment(ref _userCounter);
        var email = $"testuser{suffix}@test.com";
        const string password = "Test1234";

        await using var scope = Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email };
        await userManager.CreateAsync(user, password);

        return (email, password);
    }

    /// <summary>
    /// Returnerar konventions-ID för den seedade konventionen.
    /// </summary>
    public async Task<Guid> GetConventionIdAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var convention = await db.Conventions.FirstAsync();
        return convention.Id.Value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sql.GetConnectionString(),
                ["Jwt:Key"] = "integration-test-secret-key-minimum-32-chars",
                ["Jwt:Issuer"] = "ConventionSystem",
                ["Jwt:Audience"] = "ConventionSystem",
                ["DevData:EnableSeeding"] = "false"
            });
        });
    }

    // Skapar minimal testdata: en konvention och en admin-användare.
    // Använder CreateConventionCommand (kräver ej auth) och UserManager direkt.
    private async Task SetupConventionAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var sender = sp.GetRequiredService<ISender>();
        var personRepo = sp.GetRequiredService<IPersonRepository>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var conventionId = Guid.CreateVersion7();

        await sender.Send(new CreateConventionCommand(
            "Test Convention", "test", "Admin Test", AdminEmail, conventionId));

        var adminPerson = await personRepo.FindByEmailInConventionAsync(
            new ConventionId(conventionId), AdminEmail);

        var user = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            PersonId = adminPerson!.Id.Value
        };
        await userManager.CreateAsync(user, AdminPassword);
    }
}
