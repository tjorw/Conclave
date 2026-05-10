using ConventionSystem.Domain.Content.Entities;
using ConventionSystem.Domain.Content.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Content;

public sealed class PageTranslationConfiguration : IEntityTypeConfiguration<PageTranslation>
{
    public void Configure(EntityTypeBuilder<PageTranslation> builder)
    {
        builder.ToTable("page_translations");

        builder.Property<PageId>("PageId")
            .HasConversion(id => id.Value, value => new PageId(value))
            .HasColumnName("page_id");

        builder.HasKey("PageId", nameof(PageTranslation.Locale));

        builder.Property(t => t.Locale)
            .HasColumnName("locale")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(t => t.Content)
            .HasColumnName("content")
            .HasMaxLength(20_000)
            .IsRequired();

        builder.HasIndex("PageId").HasDatabaseName("IX_page_translations_page_id");
    }
}
