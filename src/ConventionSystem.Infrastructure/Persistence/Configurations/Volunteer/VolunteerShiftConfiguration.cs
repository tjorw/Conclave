using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Volunteer.Aggregates;
using ConventionSystem.Domain.Volunteer.Entities;
using ConventionSystem.Domain.Volunteer.Enums;
using ConventionSystem.Domain.Volunteer.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Volunteer;

public sealed class VolunteerShiftConfiguration : IEntityTypeConfiguration<VolunteerShift>
{
    public void Configure(EntityTypeBuilder<VolunteerShift> builder)
    {
        builder.ToTable("volunteer_shifts");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new VolunteerShiftId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(s => s.StationId)
            .HasConversion(id => id.Value, value => new StationId(value))
            .HasColumnName("station_id");

        builder.OwnsOne(s => s.TimeSlot, ts =>
        {
            ts.Property(t => t.Start).HasColumnName("start_time").IsRequired();
            ts.Property(t => t.End).HasColumnName("end_time").IsRequired();
        });

        builder.OwnsOne(s => s.StaffingRequirement, sr =>
        {
            sr.Property(r => r.MinPersons).HasColumnName("min_persons");
            sr.Property(r => r.MaxPersons).HasColumnName("max_persons");
        });

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasMany(s => s.Assignments)
            .WithOne()
            .HasForeignKey("VolunteerShiftId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Assignments).HasField("_assignments");
    }
}

public sealed class VolunteerAssignmentConfiguration : IEntityTypeConfiguration<VolunteerAssignment>
{
    public void Configure(EntityTypeBuilder<VolunteerAssignment> builder)
    {
        builder.ToTable("volunteer_assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new VolunteerAssignmentId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(a => a.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(a => a.AssignedById)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("assigned_by_id");

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.AssignedAt).HasColumnName("assigned_at");
    }
}
