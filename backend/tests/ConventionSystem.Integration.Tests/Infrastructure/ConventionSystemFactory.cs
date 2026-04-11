using ConventionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;

namespace ConventionSystem.Integration.Tests.Infrastructure;

public sealed class ConventionSystemFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string GetConnectionString(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = databaseName
        };
        return builder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        await MigrateAsync<SystemDbContext>(GetConnectionString("ConventionSystemRegistry"));
        await MigrateAsync<ApplicationIdentityDbContext>(GetConnectionString("ConventionSystemIdentity"));
    }

    public new async Task DisposeAsync()
    {
        await _sql.DisposeAsync();
        await base.DisposeAsync();
    }

    // Kör EF-migrationer för en ConventionDb mot en godtycklig connection string.
    // Anropas av tester innan POST /system/conventions – databasen måste finnas
    // innan tenant-kontexten löses och ConventionDbContext byggs.
    public async Task MigrateConventionDbAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ConventionDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var ctx = new ConventionDbContext(options);
        await ctx.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Ersätter connection strings från appsettings med test-containerns
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SystemDb"] = GetConnectionString("ConventionSystemRegistry"),
                ["ConnectionStrings:IdentityDb"] = GetConnectionString("ConventionSystemIdentity")
            });
        });
    }

    private static async Task MigrateAsync<TContext>(string connectionString)
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var ctx = (TContext)Activator.CreateInstance(typeof(TContext), options)!;
        await ctx.Database.MigrateAsync();
    }
}
