using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Convention;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("persons");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasDefaultValueSql("newsequentialid()");

        builder.Property(p => p.ConventionId)
            .HasConversion(id => id.Value, value => new ConventionId(value))
            .HasColumnName("convention_id");

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(320).IsRequired();
        builder.Property(p => p.Phone).HasMaxLength(30);
    }
}
