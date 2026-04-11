using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConventionSystem.Infrastructure.Persistence;

/// <summary>
/// Används av dotnet-ef vid migrationer. Kräver ingen riktig databasanslutning.
/// </summary>
public sealed class ApplicationIdentityDbContextFactory : IDesignTimeDbContextFactory<ApplicationIdentityDbContext>
{
    public ApplicationIdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationIdentityDbContext>()
            .UseSqlServer("Server=.;Database=ConventionSystemIdentity;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new ApplicationIdentityDbContext(options);
    }
}
