using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class EditionContentConfiguration : IEntityTypeConfiguration<EditionContent>
{
    public void Configure(EntityTypeBuilder<EditionContent> builder)
    {
        builder.ToTable("edition_content");

        builder.Property<EditionId>("EditionId")
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.HasKey("EditionId", nameof(EditionContent.Key));

        builder.Property(c => c.Key)
            .HasColumnName("key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Value)
            .HasColumnName("value")
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex("EditionId").HasDatabaseName("IX_edition_content_edition_id");
    }
}
