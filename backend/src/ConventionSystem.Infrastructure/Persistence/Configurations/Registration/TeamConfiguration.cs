using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Registration;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TeamId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.EditionId)
            .HasConversion(id => id.Value, value => new EditionId(value))
            .HasColumnName("edition_id");

        builder.Property(t => t.CaptainPersonId)
            .HasConversion(id => id.Value, value => new PersonId(value))
            .HasColumnName("captain_person_id");

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(t => t.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(t => t.EditionId).HasDatabaseName("IX_teams_edition_id");
        builder.HasIndex(t => t.CaptainPersonId).HasDatabaseName("IX_teams_captain_person_id");
    }
}
