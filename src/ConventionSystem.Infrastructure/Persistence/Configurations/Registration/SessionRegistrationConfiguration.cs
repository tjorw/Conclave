using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class SessionRegistrationConfiguration : IEntityTypeConfiguration<SessionRegistration>
{
    public void Configure(EntityTypeBuilder<SessionRegistration> builder)
    {
        builder.ToTable("session_registrations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new SessionRegistrationId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(r => r.SessionId)
            .HasConversion(id => id.Value, value => new SessionId(value))
            .HasColumnName("session_id");

        builder.Property(r => r.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(r => r.TicketId)
            .HasConversion(id => id.Value, value => new TicketId(value))
            .HasColumnName("ticket_id");

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
    }
}
