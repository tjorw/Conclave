using ConventionSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence;

public sealed class ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.UserType)
                .HasColumnName("user_type")
                .HasConversion<int>()
                .IsRequired();

            e.Property(u => u.TenantId)
                .HasColumnName("tenant_id");

            e.Property(u => u.PersonId).HasColumnName("person_id");
            e.HasIndex(u => u.PersonId).HasDatabaseName("IX_users_person_id");

            e.HasIndex(u => u.NormalizedUserName)
                .HasDatabaseName("UserNameIndex")
                .IsUnique(false);

            e.HasIndex(u => new { u.NormalizedEmail, u.TenantId })
                .HasDatabaseName("UX_users_tenant_email")
                .IsUnique()
                .HasFilter("[user_type] = 0 AND [NormalizedEmail] IS NOT NULL AND [tenant_id] IS NOT NULL");

            e.HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("UX_users_systemadmin_email")
                .IsUnique()
                .HasFilter("[user_type] = 1 AND [NormalizedEmail] IS NOT NULL");
        });
    }
}
