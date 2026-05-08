using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class TeamEventRegistrationConfiguration : IEntityTypeConfiguration<TeamEventRegistration>
{
    public void Configure(EntityTypeBuilder<TeamEventRegistration> builder)
    {
        builder.ToTable("team_event_registrations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new TeamEventRegistrationId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.TeamId)
            .HasConversion(id => id.Value, value => new TeamId(value))
            .HasColumnName("team_id");

        builder.Property(r => r.EventId)
            .HasConversion(id => id.Value, value => new EventId(value))
            .HasColumnName("event_id");

        builder.Property(r => r.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(r => r.TeamId).HasDatabaseName("IX_team_event_registrations_team_id");
        builder.HasIndex(r => r.EventId).HasDatabaseName("IX_team_event_registrations_event_id");
        builder.HasIndex(r => new { r.TeamId, r.EventId })
            .IsUnique()
            .HasDatabaseName("UX_team_event_registrations_team_event");
    }
}
