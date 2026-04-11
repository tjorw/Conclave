using ConventionSystem.Infrastructure.System;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence;

public sealed class SystemDbContext(DbContextOptions<SystemDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(t => t.Id);
            e.Property(t => t.Slug).HasMaxLength(100).IsRequired();
            e.Property(t => t.ConnectionString).HasMaxLength(500).IsRequired();
            e.Property(t => t.Domain).HasMaxLength(200);
            e.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("IX_tenants_slug");
            e.HasIndex(t => t.Domain).HasDatabaseName("IX_tenants_domain");
        });
    }
}
