using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations;

public sealed class DomainEventLogConfiguration : IEntityTypeConfiguration<DomainEventLogEntry>
{
    public void Configure(EntityTypeBuilder<DomainEventLogEntry> builder)
    {
        builder.ToTable("domain_event_log");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.EventType).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Payload).IsRequired();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(e => e.OccurredAt).HasDatabaseName("ix_domain_event_log_occurred_at");
    }
}
