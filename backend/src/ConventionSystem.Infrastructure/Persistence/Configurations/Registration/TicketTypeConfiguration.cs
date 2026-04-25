using System.Text.Json;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("ticket_types");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TicketTypeId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Price).IsRequired();
        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(10000);

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.ValidDays)
            .HasColumnName("valid_days")
            .HasConversion(new ValueConverter<IReadOnlyList<DateOnly>?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<DateOnly[]>(v, (JsonSerializerOptions?)null)));

        builder.Property(t => t.AllowedCategories)
            .HasColumnName("allowed_categories")
            .HasConversion(new ValueConverter<Guid[]?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Guid[]>(v, (JsonSerializerOptions?)null)));

        builder.HasIndex(t => t.EditionId).HasDatabaseName("IX_ticket_types_edition_id");
    }
}
