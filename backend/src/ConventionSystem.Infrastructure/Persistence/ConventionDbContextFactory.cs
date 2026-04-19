using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Infrastructure.Persistence;

/// <summary>
/// Används av dotnet-ef vid migrationer. Kräver ingen riktig databasanslutning.
/// </summary>
public sealed class ConventionDbContextFactory : IDesignTimeDbContextFactory<ConventionDbContext>
{
    public ConventionDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args);

        var options = new DbContextOptionsBuilder<ConventionDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ConventionDbContext(
            options,
            new DesignTimeTenantContext(),
            Options.Create(new MultitenancyOptions { Enabled = false, DefaultSubdomain = "default" }));
    }

    private static string ResolveConnectionString(string[] args)
    {
        var fromArgs = GetArgumentValue(args, "--connection");
        if (!string.IsNullOrWhiteSpace(fromArgs))
        {
            return fromArgs;
        }

        var fromEnvironment =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CONVENTIONSYSTEM_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        return "Server=.;Database=ConventionSystem;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    private static string? GetArgumentValue(string[] args, string argumentName)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
    }
}
