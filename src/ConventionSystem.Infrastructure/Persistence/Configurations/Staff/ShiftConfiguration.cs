using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Entities;
using ConventionSystem.Domain.Staff.Enums;
using ConventionSystem.Domain.Staff.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Staff;

public sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("shifts");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new ShiftId(value))
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
            .HasForeignKey("ShiftId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Assignments).HasField("_assignments");

        builder.HasIndex(s => s.StationId).HasDatabaseName("IX_shifts_station_id");
    }
}

public sealed class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
{
    public void Configure(EntityTypeBuilder<StaffAssignment> builder)
    {
        builder.ToTable("staff_assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new StaffAssignmentId(value))
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
