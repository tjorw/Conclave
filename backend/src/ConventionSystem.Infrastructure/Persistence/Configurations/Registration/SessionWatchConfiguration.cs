using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class SessionWatchConfiguration : IEntityTypeConfiguration<SessionWatch>
{
    public void Configure(EntityTypeBuilder<SessionWatch> builder)
    {
        builder.ToTable("session_watches");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasConversion(id => id.Value, value => new SessionWatchId(value))
            .ValueGeneratedNever();

        builder.Property(w => w.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(w => w.SessionId)
            .HasConversion(id => id.Value, value => new SessionId(value))
            .HasColumnName("session_id");

        builder.Property(w => w.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(w => w.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(w => new { w.PersonId, w.SessionId })
            .IsUnique()
            .HasDatabaseName("IX_session_watches_person_session_unique");

        builder.HasIndex(w => w.EditionId)
            .HasDatabaseName("IX_session_watches_edition_id");
    }
}
