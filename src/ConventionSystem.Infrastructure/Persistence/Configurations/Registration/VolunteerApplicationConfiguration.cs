using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class VolunteerApplicationConfiguration : IEntityTypeConfiguration<VolunteerApplication>
{
    public void Configure(EntityTypeBuilder<VolunteerApplication> builder)
    {
        builder.ToTable("volunteer_applications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new VolunteerApplicationId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(a => a.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(a => a.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(a => a.InterestDescription)
            .HasMaxLength(2000)
            .HasColumnName("interest_description");

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at");

        builder.HasMany(a => a.Availabilities)
            .WithOne()
            .HasForeignKey("VolunteerApplicationId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(a => a.StationPreferences, sp =>
        {
            sp.ToTable("volunteer_application_stations");
            sp.WithOwner().HasForeignKey("VolunteerApplicationId");
            sp.Property<Guid>("Id").HasDefaultValueSql("newsequentialid()");
            sp.HasKey("Id");
            sp.Property(p => p.StationId)
                .HasConversion(id => id.Value, value => new StationId(value))
                .HasColumnName("station_id");
        });

        builder.Navigation(a => a.Availabilities).HasField("_availabilities");
    }
}

public sealed class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("volunteer_application_availabilities");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AvailabilityId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.OwnsOne(a => a.TimeSlot, ts =>
        {
            ts.Property(t => t.Start).HasColumnName("from").IsRequired();
            ts.Property(t => t.End).HasColumnName("to").IsRequired();
        });
    }
}
