using ConventionSystem.Domain.Event.Entities;
using ConventionSystem.Domain.Event.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Event;

public sealed class EventTranslationConfiguration : IEntityTypeConfiguration<EventTranslation>
{
    public void Configure(EntityTypeBuilder<EventTranslation> builder)
    {
        builder.ToTable("event_translations");

        builder.Property<EventId>("EventId")
            .HasConversion(id => id.Value, value => new EventId(value))
            .HasColumnName("event_id");

        builder.HasKey("EventId", nameof(EventTranslation.Locale));

        builder.Property(t => t.Locale)
            .HasColumnName("locale")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(10_000)
            .IsRequired();

        builder.HasIndex("EventId").HasDatabaseName("IX_event_translations_event_id");
    }
}
