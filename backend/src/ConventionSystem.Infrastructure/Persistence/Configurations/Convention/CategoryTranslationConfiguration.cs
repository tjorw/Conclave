using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class CategoryTranslationConfiguration : IEntityTypeConfiguration<CategoryTranslation>
{
    public void Configure(EntityTypeBuilder<CategoryTranslation> builder)
    {
        builder.ToTable("category_translations");

        builder.Property<CategoryId>("CategoryId")
            .HasConversion(id => id.Value, value => new CategoryId(value))
            .HasColumnName("category_id");

        builder.HasKey("CategoryId", nameof(CategoryTranslation.Locale));

        builder.Property(t => t.Locale)
            .HasColumnName("locale")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex("CategoryId").HasDatabaseName("IX_category_translations_category_id");
    }
}
