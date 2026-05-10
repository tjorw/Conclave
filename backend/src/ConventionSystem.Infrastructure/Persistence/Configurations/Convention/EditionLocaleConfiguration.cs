using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class EditionLocaleConfiguration : IEntityTypeConfiguration<EditionLocale>
{
    public void Configure(EntityTypeBuilder<EditionLocale> builder)
    {
        builder.ToTable("edition_locales");

        builder.Property<EditionId>("EditionId")
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.HasKey("EditionId", nameof(EditionLocale.Locale));

        builder.Property(l => l.Locale)
            .HasColumnName("locale")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(l => l.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();

        builder.HasIndex("EditionId").HasDatabaseName("IX_edition_locales_edition_id");
    }
}
