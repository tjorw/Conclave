using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConventionSystem.Infrastructure.Persistence;

/// <summary>
/// Används av dotnet-ef vid migrationer. Kräver ingen riktig databasanslutning.
/// </summary>
public sealed class ConventionDbContextFactory : IDesignTimeDbContextFactory<ConventionDbContext>
{
    public ConventionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConventionDbContext>()
            .UseSqlServer("Server=.;Database=ConventionSystem;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new ConventionDbContext(options);
    }
}
