using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TicketId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(t => t.TicketTypeId)
            .HasConversion(id => id.Value, value => new TicketTypeId(value))
            .HasColumnName("ticket_type_id");

        builder.Property(t => t.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(t => t.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(t => t.AssignedById)
            .HasConversion(id => id!.Value.Value, value => (PersonId?)new PersonId(value))
            .HasColumnName("assigned_by_id");

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.CollectedById)
            .HasConversion(id => id!.Value.Value, value => (PersonId?)new PersonId(value))
            .HasColumnName("collected_by_id");

        builder.Property(t => t.CollectedAt).HasColumnName("collected_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(t => t.EditionId).HasDatabaseName("IX_tickets_edition_id");
        builder.HasIndex(t => t.PersonId).HasDatabaseName("IX_tickets_person_id");
        builder.HasIndex(t => t.TicketTypeId).HasDatabaseName("IX_tickets_ticket_type_id");
    }
}
