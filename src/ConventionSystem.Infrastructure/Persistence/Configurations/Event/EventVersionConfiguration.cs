using ConventionSystem.Domain.Event.Entities;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Event;

public sealed class EventVersionConfiguration : IEntityTypeConfiguration<EventVersion>
{
    public void Configure(EntityTypeBuilder<EventVersion> builder)
    {
        builder.ToTable("event_versions");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasConversion(id => id.Value, value => new EventVersionId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(v => v.EventId)
            .HasConversion(id => id.Value, value => new EventId(value))
            .HasColumnName("event_id");

        builder.Property(v => v.Title).HasMaxLength(300).IsRequired(false);
        builder.Property(v => v.Description).HasMaxLength(5000).IsRequired(false);

        builder.Property(v => v.RegistrationType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("registration_type");

        builder.Property(v => v.DropInRules)
            .HasMaxLength(2000)
            .HasColumnName("drop_in_rules");

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(v => v.CreatedAt).HasColumnName("created_at");

        builder.HasMany(v => v.SessionRequests)
            .WithOne()
            .HasForeignKey("EventVersionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.SessionRequests).HasField("_sessionRequests");
    }
}

public sealed class SessionRequestConfiguration : IEntityTypeConfiguration<SessionRequest>
{
    public void Configure(EntityTypeBuilder<SessionRequest> builder)
    {
        builder.ToTable("session_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new SessionRequestId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(r => r.Description).HasMaxLength(1000).IsRequired();

        builder.Property(r => r.RequestedDurationMinutes)
            .HasColumnName("requested_duration_minutes");

        builder.Property(r => r.RequestedSeats)
            .HasColumnName("requested_seats");

        builder.Property(r => r.StartType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("start_type");
    }
}
