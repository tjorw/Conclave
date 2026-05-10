using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class ProgramTagTranslationConfiguration : IEntityTypeConfiguration<ProgramTagTranslation>
{
    public void Configure(EntityTypeBuilder<ProgramTagTranslation> builder)
    {
        builder.ToTable("program_tag_translations");

        builder.Property<EditionId>("EditionId")
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.HasKey("EditionId", nameof(ProgramTagTranslation.TagName), nameof(ProgramTagTranslation.Locale));

        builder.Property(t => t.TagName)
            .HasColumnName("tag_name")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.Locale)
            .HasColumnName("locale")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.TranslatedName)
            .HasColumnName("translated_name")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex("EditionId").HasDatabaseName("IX_program_tag_translations_edition_id");
    }
}
