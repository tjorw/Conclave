using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("ticket_types");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TicketTypeId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(t => t.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Price).IsRequired();

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasMany(t => t.Perks)
            .WithOne()
            .HasForeignKey("TicketTypeId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Perks).HasField("_perks");
    }
}

public sealed class TicketPerkConfiguration : IEntityTypeConfiguration<TicketPerk>
{
    public void Configure(EntityTypeBuilder<TicketPerk> builder)
    {
        builder.ToTable("ticket_perks");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new TicketPerkId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(p => p.Description).HasMaxLength(500).IsRequired();
    }
}
