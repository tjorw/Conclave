using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class EditionConfiguration : IEntityTypeConfiguration<Edition>
{
    public void Configure(EntityTypeBuilder<Edition> builder)
    {
        builder.ToTable("editions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(e => e.ConventionId)
            .HasConversion(id => id.Value, value => new ConventionId(value))
            .HasColumnName("convention_id");

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();

        builder.OwnsOne(e => e.Period, period =>
        {
            period.Property(p => p.StartDate).HasColumnName("start_date").IsRequired();
            period.Property(p => p.EndDate).HasColumnName("end_date").IsRequired();
        });

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.OrganiserRegistrationOpen).HasColumnName("organiser_registration_open");
        builder.Property(e => e.VolunteerRegistrationOpen).HasColumnName("volunteer_registration_open");
        builder.Property(e => e.VisitorRegistrationOpen).HasColumnName("visitor_registration_open");

        builder.Property(e => e.VolunteerCoordinatorId)
            .HasConversion(id => id!.Value.Value, value => (PersonId?)new PersonId(value))
            .HasColumnName("volunteer_coordinator_id");

        builder.Property(e => e.EventCoordinatorId)
            .HasConversion(id => id!.Value.Value, value => (PersonId?)new PersonId(value))
            .HasColumnName("event_coordinator_id");

        builder.HasMany(e => e.Venues).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.Stations).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.Categories).WithOne().HasForeignKey("EditionId").IsRequired();

        builder.Navigation(e => e.Venues).HasField("_venues");
        builder.Navigation(e => e.Stations).HasField("_stations");
        builder.Navigation(e => e.Categories).HasField("_categories");
    }
}

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasConversion(id => id.Value, value => new VenueId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Building).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Description).HasMaxLength(1000);
    }
}

public sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("stations");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new StationId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(s => s.ResponsibleId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("responsible_id");

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CategoryId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(c => c.ResponsibleId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("responsible_id");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
    }
}
