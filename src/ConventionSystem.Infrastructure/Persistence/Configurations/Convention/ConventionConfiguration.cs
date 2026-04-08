using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class ConventionConfiguration : IEntityTypeConfiguration<Domain.Convention.Aggregates.Convention>
{
    public void Configure(EntityTypeBuilder<Domain.Convention.Aggregates.Convention> builder)
    {
        builder.ToTable("conventions");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new ConventionId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.HasMany(c => c.Administrators)
            .WithOne()
            .HasForeignKey("ConventionId")
            .IsRequired();

        builder.Navigation(c => c.Administrators).HasField("_administrators");
    }
}

public sealed class ConventionAdministratorConfiguration : IEntityTypeConfiguration<ConventionAdministrator>
{
    public void Configure(EntityTypeBuilder<ConventionAdministrator> builder)
    {
        builder.ToTable("convention_administrators");

        builder.Property<ConventionId>("ConventionId")
            .HasConversion(id => id.Value, value => new ConventionId(value));
        builder.HasKey("ConventionId", nameof(ConventionAdministrator.PersonId));

        builder.Property(a => a.PersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("person_id");

        builder.Property(a => a.AddedById)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("added_by_id");

        builder.Property(a => a.AddedAt).HasColumnName("added_at");
    }
}
