using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConventionSystem.Infrastructure.Persistence.Configurations.Content;

public sealed class MailTemplateConfiguration : IEntityTypeConfiguration<MailTemplate>
{
    public void Configure(EntityTypeBuilder<MailTemplate> builder)
    {
        builder.ToTable("mail_templates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new MailTemplateId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.ConventionId)
            .HasConversion(id => id.Value, value => new ConventionId(value))
            .HasColumnName("convention_id");

        builder.Property(t => t.TemplateType)
            .HasConversion(type => type.ToString(), value => Enum.Parse<MailTemplateType>(value))
            .HasColumnName("template_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Subject)
            .HasColumnName("subject")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.BodyMarkdown)
            .HasColumnName("body_markdown")
            .IsRequired();

        builder.Property(t => t.IsCustomized)
            .HasColumnName("is_customized");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(t => new { t.ConventionId, t.TemplateType })
            .IsUnique()
            .HasDatabaseName("IX_mail_templates_convention_id_template_type");
    }
}
