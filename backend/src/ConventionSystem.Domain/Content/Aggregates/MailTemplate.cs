using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Content.Aggregates;

public sealed class MailTemplate : AggregateRoot
{
    public MailTemplateId Id { get; private set; }
    public ConventionId ConventionId { get; private set; }
    public MailTemplateType TemplateType { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string BodyMarkdown { get; private set; } = string.Empty;
    public bool IsCustomized { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private MailTemplate() { }

    public MailTemplate(
        MailTemplateId id,
        ConventionId conventionId,
        MailTemplateType templateType,
        string subject,
        string bodyMarkdown)
    {
        Id = id;
        ConventionId = conventionId;
        TemplateType = templateType;
        UpdatedAt = DateTimeOffset.UtcNow;
        Customize(subject, bodyMarkdown);
    }

    public void Customize(string subject, string bodyMarkdown)
    {
        var trimmedSubject = subject.Trim();
        if (trimmedSubject.Length is < 1 or > 500)
            throw new ArgumentException("Ämnesrad måste anges och vara max 500 tecken.", nameof(subject));

        var trimmedBody = bodyMarkdown.Trim();
        if (trimmedBody.Length > 20_000)
            throw new ArgumentException("Brödtext får inte vara längre än 20 000 tecken.", nameof(bodyMarkdown));

        Subject = trimmedSubject;
        BodyMarkdown = trimmedBody;
        IsCustomized = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ResetToDefault(string defaultSubject, string defaultBodyMarkdown)
    {
        Subject = defaultSubject;
        BodyMarkdown = defaultBodyMarkdown;
        IsCustomized = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
