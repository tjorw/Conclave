using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CreateConvention;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Application.Common;
using ConventionSystem.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Testcontainers.MsSql;

namespace ConventionSystem.Integration.Tests.Infrastructure;

public sealed class ConventionSystemFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();
    private readonly string _uploadRoot = Path.Combine(
        Path.GetTempPath(),
        "convention-system-upload-tests",
        Guid.NewGuid().ToString("N"));

    private static int _userCounter;

    public const string AdminEmail = "admin@test.se";
    public const string AdminPassword = "Admin123!";

    public Guid SeededConventionId { get; private set; }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        // Trigga uppstart – kör migrationer för ConventionDbContext och ApplicationIdentityDbContext
        _ = Server;

        await SetupConventionAsync();
    }

    public new async Task DisposeAsync()
    {
        if (Directory.Exists(_uploadRoot))
            Directory.Delete(_uploadRoot, recursive: true);

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
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            UserType = UserType.TenantUser,
            TenantId = Guid.Empty,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, password);

        return (email, password);
    }

    /// <summary>
    /// Returnerar konventions-ID för den seedade konventionen.
    /// </summary>
    public Task<Guid> GetConventionIdAsync() => Task.FromResult(SeededConventionId);

    // JWT-nyckeln som används för att signera tokens i testerna.
    // Måste sättas explicit via PostConfigure eftersom JWT-middleware läser sin nyckel
    // vid uppstart (från builder.Configuration), INNAN factory:ns in-memory-config hinner appliceras.
    internal const string TestJwtKey = "integration-test-secret-key-minimum-32-chars";
    internal const string TestJwtIssuer = "ConventionSystem";
    internal const string TestJwtAudience = "ConventionSystem";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sql.GetConnectionString(),
                [JwtOptions.KeyConfigurationKey] = TestJwtKey,
                [JwtOptions.IssuerConfigurationKey] = TestJwtIssuer,
                [JwtOptions.AudienceConfigurationKey] = TestJwtAudience,
                ["DevData:EnableSeeding"] = "false",
                ["Email:Provider"] = "Logging",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Migrations"] = "Warning",
                ["UseHttpsRedirect"] = "false",
                ["Multitenancy:Enabled"] = "false",
                ["FileStorage:Provider"] = "Local",
                ["FileStorage:MaxSizeMb"] = "5",
                ["FileStorage:LocalRootPath"] = _uploadRoot
            });
        });

        // JWT-middleware konfigurerar sin IssuerSigningKey vid uppstart, innan in-memory-config
        // är på plats. PostConfigure ser till att valideringsnyckeln matchar testkonfigurationen.
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
                options.TokenValidationParameters.ValidIssuer = TestJwtIssuer;
                options.TokenValidationParameters.ValidAudience = TestJwtAudience;
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

        SeededConventionId = Guid.CreateVersion7();

        await sender.Send(new CreateConventionCommand(
            "Test Convention", "test", "Admin Test", AdminEmail, SeededConventionId));

        var adminPerson = await personRepo.FindByEmailInConventionAsync(
            new ConventionId(SeededConventionId), AdminEmail);

        var user = new ApplicationUser
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            UserType = UserType.TenantUser,
            TenantId = Guid.Empty,
            PersonId = adminPerson!.Id.Value,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, AdminPassword);
    }
}
