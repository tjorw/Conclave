using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("staff_application_availabilities");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AvailabilityId(value))
            .ValueGeneratedNever();

        builder.OwnsOne(a => a.TimeSlot, ts =>
        {
            ts.Property(t => t.Start).HasColumnName("start").IsRequired();
            ts.Property(t => t.End).HasColumnName("end").IsRequired();
        });
    }
}

public sealed class StaffApplicationConfiguration : IEntityTypeConfiguration<StaffApplication>
{
    public void Configure(EntityTypeBuilder<StaffApplication> builder)
    {
        builder.ToTable("staff_applications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new StaffApplicationId(value))
            .ValueGeneratedNever();

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
            .HasForeignKey("StaffApplicationId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(a => a.StaffAreaPreferences, sp =>
        {
            sp.ToTable("staff_application_staff_areas");
            sp.WithOwner().HasForeignKey("StaffApplicationId");
            sp.Property<Guid>("Id").HasDefaultValueSql("newsequentialid()");
            sp.HasKey("Id");
            sp.Property<Guid>("TenantId").HasColumnName("tenant_id");
            sp.Property(p => p.StaffAreaId)
                .HasConversion(id => id.Value, value => new StaffAreaId(value))
                .HasColumnName("staff_area_id");

            sp.HasIndex("TenantId");
        });

        builder.Navigation(a => a.Availabilities).HasField("_availabilities");

        builder.HasIndex(a => a.EditionId).HasDatabaseName("IX_staff_applications_edition_id");
        builder.HasIndex(a => a.PersonId).HasDatabaseName("IX_staff_applications_person_id");
    }
}
