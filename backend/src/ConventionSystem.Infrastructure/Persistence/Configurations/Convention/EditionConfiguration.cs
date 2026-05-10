using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReceptionStaffEntity = ConventionSystem.Domain.Convention.Entities.ReceptionStaff;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class EditionConfiguration : IEntityTypeConfiguration<Edition>
{
    public void Configure(EntityTypeBuilder<Edition> builder)
    {
        builder.ToTable("editions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .ValueGeneratedNever();

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
        builder.Property(e => e.StaffRegistrationOpen).HasColumnName("staff_registration_open");
        builder.Property(e => e.VisitorRegistrationOpen).HasColumnName("visitor_registration_open");

        builder.Property(e => e.StaffCoordinatorId)
            .HasConversion(id => id!.Value.Value, value => (PersonId?)new PersonId(value))
            .HasColumnName("staff_coordinator_id");

        builder.Property(e => e.EventCoordinatorId)
            .HasConversion(id => id!.Value.Value, value => (PersonId?)new PersonId(value))
            .HasColumnName("event_coordinator_id");

        builder.OwnsMany(e => e.ProgramTagDefinitions, tags =>
        {
            tags.ToTable("edition_program_tag_definitions");
            tags.WithOwner().HasForeignKey("EditionId");
            tags.Property<EditionId>("EditionId")
                .HasConversion(id => id.Value, value => new EditionId(value))
                .HasColumnName("edition_id");

            tags.Property<Guid>("Id").HasDefaultValueSql("newsequentialid()");
            tags.HasKey("Id");

            tags.Property(t => t.Name)
                .HasColumnName("name")
                .HasMaxLength(64)
                .IsRequired();

            tags.Property<Guid>("TenantId").HasColumnName("tenant_id");
            tags.HasIndex("EditionId", "Name").IsUnique().HasDatabaseName("IX_edition_program_tag_definitions_edition_id_name");
            tags.HasIndex("TenantId").HasDatabaseName("IX_edition_program_tag_definitions_tenant_id");
        });

        builder.HasMany(e => e.Venues).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.StaffAreas).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.Stations).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.Categories).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.ScheduleDays).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.ReceptionStaff).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.Content).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.Locales).WithOne().HasForeignKey("EditionId").IsRequired();
        builder.HasMany(e => e.ProgramTagTranslations).WithOne().HasForeignKey("EditionId").IsRequired().OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Venues).HasField("_venues");
        builder.Navigation(e => e.StaffAreas).HasField("_staffAreas");
        builder.Navigation(e => e.Stations).HasField("_stations");
        builder.Navigation(e => e.Categories).HasField("_categories");
        builder.Navigation(e => e.ProgramTagDefinitions).HasField("_programTagDefinitions");
        builder.Navigation(e => e.ProgramTagTranslations).HasField("_programTagTranslations");
        builder.Navigation(e => e.ScheduleDays).HasField("_scheduleDays");
        builder.Navigation(e => e.ReceptionStaff).HasField("_receptionStaff");
        builder.Navigation(e => e.Content).HasField("_content");
        builder.Navigation(e => e.Locales).HasField("_locales");

        builder.HasIndex(e => e.ConventionId).HasDatabaseName("IX_editions_convention_id");
    }
}

public sealed class EditionScheduleDayConfiguration : IEntityTypeConfiguration<EditionScheduleDay>
{
    public void Configure(EntityTypeBuilder<EditionScheduleDay> builder)
    {
        builder.ToTable("edition_schedule_days");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(d => d.StartTime)
            .HasColumnName("start_time");

        builder.Property(d => d.EndTime)
            .HasColumnName("end_time");

        builder.HasIndex("EditionId", nameof(EditionScheduleDay.Date))
            .IsUnique()
            .HasDatabaseName("IX_edition_schedule_days_edition_id_date");
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
            .ValueGeneratedNever();

        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Building).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Description).HasMaxLength(1000);
    }
}

public sealed class StaffAreaConfiguration : IEntityTypeConfiguration<StaffArea>
{
    public void Configure(EntityTypeBuilder<StaffArea> builder)
    {
        builder.ToTable("staff_areas");

        builder.HasKey(sa => sa.Id);
        builder.Property(sa => sa.Id)
            .HasConversion(id => id.Value, value => new StaffAreaId(value))
            .ValueGeneratedNever();

        builder.Property(sa => sa.ResponsibleId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("responsible_id");

        builder.Property(sa => sa.Name).HasMaxLength(200).IsRequired();
        builder.Property(sa => sa.Description).HasMaxLength(1000);

        builder.HasIndex("EditionId").HasDatabaseName("IX_staff_areas_edition_id");
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
            .ValueGeneratedNever();

        builder.Property(s => s.StaffAreaId)
            .HasConversion(id => id.Value, value => new StaffAreaId(value))
            .HasColumnName("staff_area_id");

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);

        builder.HasIndex("EditionId").HasDatabaseName("IX_stations_edition_id");
        builder.HasIndex(s => s.StaffAreaId).HasDatabaseName("IX_stations_staff_area_id");
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
            .ValueGeneratedNever();

        builder.Property(c => c.ResponsibleId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("responsible_id");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.OrganizerInstructions).HasColumnName("organizer_instructions").HasMaxLength(4000);
        builder.Property(c => c.PublicDescription).HasColumnName("public_description").HasMaxLength(4000);

        builder.HasMany(c => c.Translations).WithOne().HasForeignKey("CategoryId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Translations).HasField("_translations");
    }
}

public sealed class ReceptionStaffConfiguration : IEntityTypeConfiguration<ReceptionStaffEntity>
{
    public void Configure(EntityTypeBuilder<ReceptionStaffEntity> builder)
    {
        builder.ToTable("edition_reception_staff");

        builder.Property<EditionId>("EditionId")
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.HasKey("EditionId", nameof(ReceptionStaffEntity.PersonId));

        builder.Property(r => r.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(r => r.AddedById)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("added_by_id");

        builder.Property(r => r.AddedAt).HasColumnName("added_at");

        builder.HasIndex("EditionId").HasDatabaseName("IX_edition_reception_staff_edition_id");
    }
}
