using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasDefaultValueSql("newsequentialid()");
        builder.Property(m => m.Type).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.ProcessAfter).HasColumnName("process_after");
        builder.Property(m => m.ProcessedAt).HasColumnName("processed_at");
        builder.Property(m => m.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(m => m.Error).HasColumnName("error");

        builder.HasIndex(m => m.ProcessedAt).HasDatabaseName("ix_outbox_messages_processed_at");
    }
}
