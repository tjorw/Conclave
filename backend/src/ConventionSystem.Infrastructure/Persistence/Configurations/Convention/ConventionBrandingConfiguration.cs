using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class ConventionBrandingConfiguration : IEntityTypeConfiguration<ConventionBranding>
{
    public void Configure(EntityTypeBuilder<ConventionBranding> builder)
    {
        builder.ToTable("convention_brandings");

        builder.HasKey(b => b.ConventionId);

        builder.Property(b => b.ConventionId)
            .HasConversion(id => id.Value, value => new ConventionId(value))
            .HasColumnName("convention_id")
            .ValueGeneratedNever();

        builder.Property(b => b.PrimaryColor)
            .HasMaxLength(7)
            .HasColumnName("primary_color")
            .IsRequired();

        builder.Property(b => b.AccentColor)
            .HasMaxLength(7)
            .HasColumnName("accent_color")
            .IsRequired();

        builder.Property(b => b.LogoUrl)
            .HasMaxLength(1000)
            .HasColumnName("logo_url");

        builder.Property(b => b.FaviconUrl)
            .HasMaxLength(1000)
            .HasColumnName("favicon_url");

        builder.Property(b => b.FontFamily)
            .HasMaxLength(100)
            .HasColumnName("font_family")
            .IsRequired();

        builder.Property(b => b.CustomCss)
            .HasMaxLength(ConventionBranding.CustomCssMaxLength)
            .HasColumnName("custom_css");

        builder.HasOne<Domain.Convention.Aggregates.Convention>()
            .WithOne()
            .HasForeignKey<ConventionBranding>(b => b.ConventionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
