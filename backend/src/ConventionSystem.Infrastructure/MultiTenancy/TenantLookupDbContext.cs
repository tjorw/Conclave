using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Infrastructure.Persistence.Configurations.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public sealed class TenantLookupDbContext(DbContextOptions<TenantLookupDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
    }
}
