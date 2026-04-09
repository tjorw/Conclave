using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class VisitorRegistrationConfiguration : IEntityTypeConfiguration<VisitorRegistration>
{
    public void Configure(EntityTypeBuilder<VisitorRegistration> builder)
    {
        builder.ToTable("visitor_registrations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new VisitorRegistrationId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(r => r.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(r => r.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(r => r.TicketId)
            .HasConversion(id => id.Value, value => new TicketId(value))
            .HasColumnName("ticket_id");

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.PaymentReference)
            .HasMaxLength(200)
            .HasColumnName("payment_reference");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(r => r.EditionId).HasDatabaseName("IX_visitor_registrations_edition_id");
        builder.HasIndex(r => r.PersonId).HasDatabaseName("IX_visitor_registrations_person_id");
    }
}
