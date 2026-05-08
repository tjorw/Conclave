namespace ConventionSystem.Domain.Content.Ids;

public readonly record struct MailTemplateId(Guid Value)
{
    public static MailTemplateId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
