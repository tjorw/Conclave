using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Content;

public sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PageId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.ConventionId)
            .HasConversion(id => id.Value, value => new ConventionId(value))
            .HasColumnName("convention_id");

        builder.Property(p => p.EditionId)
            .HasConversion(id => id!.Value.Value, value => (EditionId?)new EditionId(value))
            .HasColumnName("edition_id")
            .IsRequired(false);

        builder.Property(p => p.Slug)
            .HasMaxLength(200)
            .HasColumnName("slug")
            .IsRequired();

        builder.Property(p => p.Title)
            .HasMaxLength(300)
            .HasColumnName("title")
            .IsRequired();

        builder.Property(p => p.Content)
            .HasMaxLength(20000)
            .HasColumnName("content")
            .IsRequired(false);

        builder.Property(p => p.IsPublished).HasColumnName("is_published");
        builder.Property(p => p.ShowInPublicMenu).HasColumnName("show_in_public_menu");
        builder.Property(p => p.MenuSortOrder)
            .HasColumnName("menu_sort_order")
            .HasDefaultValue(0);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(p => new { p.ConventionId, p.EditionId, p.Slug })
            .IsUnique()
            .HasFilter("[edition_id] IS NOT NULL")
            .HasDatabaseName("IX_pages_convention_id_edition_id_slug");

        builder.HasIndex(p => new { p.ConventionId, p.Slug })
            .IsUnique()
            .HasFilter("[edition_id] IS NULL")
            .HasDatabaseName("IX_pages_convention_id_slug");
    }
}
