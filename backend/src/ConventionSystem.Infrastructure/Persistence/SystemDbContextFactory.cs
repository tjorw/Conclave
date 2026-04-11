using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConventionSystem.Infrastructure.Persistence;

/// <summary>
/// Används av dotnet-ef vid migrationer. Kräver ingen riktig databasanslutning.
/// </summary>
public sealed class SystemDbContextFactory : IDesignTimeDbContextFactory<SystemDbContext>
{
    public SystemDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SystemDbContext>()
            .UseSqlServer("Server=.;Database=ConventionSystemRegistry;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new SystemDbContext(options);
    }
}
