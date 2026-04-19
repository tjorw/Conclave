using System.Text.Json;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class PromotionCodeConfiguration : IEntityTypeConfiguration<PromotionCode>
{
    public void Configure(EntityTypeBuilder<PromotionCode> builder)
    {
        builder.ToTable("promotion_codes");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PromotionCodeId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(p => p.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("discount_type");

        builder.Property(p => p.DiscountValue)
            .HasColumnName("discount_value");

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active");

        builder.Property(p => p.MaxRedemptions)
            .HasColumnName("max_redemptions");

        builder.Property(p => p.ValidFrom)
            .HasColumnName("valid_from");

        builder.Property(p => p.ValidUntil)
            .HasColumnName("valid_until");

        builder.Property(p => p.AllowedTicketTypeIds)
            .HasColumnName("allowed_ticket_type_ids")
            .HasConversion(new ValueConverter<Guid[]?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Guid[]>(v, (JsonSerializerOptions?)null)));

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.HasMany(p => p.Redemptions)
            .WithOne()
            .HasForeignKey(r => r.PromotionCodeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Redemptions).HasField("_redemptions");

        builder.HasIndex(p => new { p.EditionId, p.Code })
            .IsUnique()
            .HasDatabaseName("IX_promotion_codes_edition_id_code");
    }
}

public sealed class PromotionCodeRedemptionConfiguration : IEntityTypeConfiguration<PromotionCodeRedemption>
{
    public void Configure(EntityTypeBuilder<PromotionCodeRedemption> builder)
    {
        builder.ToTable("promotion_code_redemptions");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new PromotionCodeRedemptionId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.PromotionCodeId)
            .HasConversion(id => id.Value, value => new PromotionCodeId(value))
            .HasColumnName("promotion_code_id");

        builder.Property(r => r.TicketId)
            .HasConversion(id => id.Value, value => new TicketId(value))
            .HasColumnName("ticket_id");

        builder.Property(r => r.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(r => r.TicketTypeId)
            .HasConversion(id => id.Value, value => new TicketTypeId(value))
            .HasColumnName("ticket_type_id");

        builder.Property(r => r.DiscountApplied)
            .HasColumnName("discount_applied");

        builder.Property(r => r.FinalPrice)
            .HasColumnName("final_price");

        builder.Property(r => r.RedeemedAt)
            .HasColumnName("redeemed_at");

        builder.HasIndex(r => r.PromotionCodeId)
            .HasDatabaseName("IX_promotion_code_redemptions_promotion_code_id");
    }
}
