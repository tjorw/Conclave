using ConventionSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence;

public sealed class ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ConventionUserLink> ConventionUserLinks => Set<ConventionUserLink>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ConventionUserLink>(e =>
        {
            e.ToTable("convention_user_links");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasDefaultValueSql("newsequentialid()");
            e.Property(l => l.UserId).IsRequired();
            e.HasOne(l => l.User)
                .WithMany(u => u.ConventionLinks)
                .HasForeignKey(l => l.UserId);
            e.HasIndex(l => new { l.UserId, l.ConventionId })
                .IsUnique()
                .HasDatabaseName("IX_convention_user_links_user_id_convention_id");
            e.HasIndex(l => l.PersonId)
                .HasDatabaseName("IX_convention_user_links_person_id");
        });
    }
}
