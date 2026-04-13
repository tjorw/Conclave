using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Aggregates;
using ConventionSystem.Domain.Event.Entities;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Event;

public sealed class EventConfiguration : IEntityTypeConfiguration<Domain.Event.Aggregates.Event>
{
    public void Configure(EntityTypeBuilder<Domain.Event.Aggregates.Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EventId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(e => e.CategoryId)
            .HasConversion(id => id.Value, value => new CategoryId(value))
            .HasColumnName("category_id");

        builder.Property(e => e.LeadOrganiserId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("lead_organiser_id");

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Title)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(e => e.Description)
            .HasMaxLength(5000)
            .IsRequired(false);

        builder.Property(e => e.RegistrationType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("registration_type");

        builder.Property(e => e.DropInRules)
            .HasMaxLength(2000)
            .HasColumnName("drop_in_rules");

        builder.HasMany(e => e.SessionRequests)
            .WithOne()
            .HasForeignKey("EventId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey("EventId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.CoOrganisers)
            .WithOne()
            .HasForeignKey("EventId")
            .IsRequired();

        builder.HasMany(e => e.Comments)
            .WithOne()
            .HasForeignKey(c => c.EventId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.SessionRequests).HasField("_sessionRequests");
        builder.Navigation(e => e.Sessions).HasField("_sessions");
        builder.Navigation(e => e.CoOrganisers).HasField("_coOrganisers");
        builder.Navigation(e => e.Comments).HasField("_comments");

        builder.HasIndex(e => e.EditionId).HasDatabaseName("IX_events_edition_id");
        builder.HasIndex(e => e.CategoryId).HasDatabaseName("IX_events_category_id");
        builder.HasIndex(e => e.LeadOrganiserId).HasDatabaseName("IX_events_lead_organiser_id");
    }
}

public sealed class CoOrganiserConfiguration : IEntityTypeConfiguration<CoOrganiser>
{
    public void Configure(EntityTypeBuilder<CoOrganiser> builder)
    {
        builder.ToTable("co_organisers");

        builder.Property<EventId>("EventId")
            .HasConversion(id => id.Value, value => new EventId(value));
        builder.HasKey("EventId", nameof(CoOrganiser.PersonId));

        builder.Property(c => c.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(c => c.AddedAt).HasColumnName("added_at");
    }
}

public sealed class EventCommentConfiguration : IEntityTypeConfiguration<EventComment>
{
    public void Configure(EntityTypeBuilder<EventComment> builder)
    {
        builder.ToTable("event_comments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new EventCommentId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.EventId)
            .HasConversion(id => id.Value, value => new EventId(value))
            .HasColumnName("event_id");

        builder.Property(c => c.AuthorId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("author_id");

        builder.Property(c => c.Text).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
    }
}

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new SessionId(value))
            .ValueGeneratedNever();

        builder.Property(s => s.EventId)
            .HasConversion(id => id.Value, value => new EventId(value))
            .HasColumnName("event_id");

        builder.Property(s => s.VenueId)
            .HasConversion(id => id.Value, value => new VenueId(value))
            .HasColumnName("venue_id");

        builder.OwnsOne(s => s.TimeSlot, ts =>
        {
            ts.Property(t => t.Start).HasColumnName("start_time").IsRequired();
            ts.Property(t => t.End).HasColumnName("end_time").IsRequired();
        });

        builder.Property(s => s.MaxSeats).HasColumnName("max_seats");

        builder.Property(s => s.StartType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("start_type");

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(s => s.VenueId).HasDatabaseName("IX_sessions_venue_id");
        builder.HasIndex("EventId").HasDatabaseName("IX_sessions_event_id");
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
            .ValueGeneratedNever();

        builder.Property(r => r.Description).HasMaxLength(1000).IsRequired();

        builder.Property(r => r.RequestedDurationMinutes)
            .HasColumnName("requested_duration_minutes");

        builder.Property(r => r.RequestedSeats)
            .HasColumnName("requested_seats");

        builder.Property(r => r.StartType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("start_type");

        builder.HasIndex("EventId").HasDatabaseName("IX_session_requests_event_id");
    }
}
