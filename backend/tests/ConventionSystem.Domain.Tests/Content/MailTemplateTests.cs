using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Tests.Content;

public sealed class MailTemplateTests
{
    [Fact]
    public void Constructor_SetsIsCustomizedToTrue()
    {
        var template = CreateTemplate();

        Assert.True(template.IsCustomized);
    }

    [Fact]
    public void Constructor_SetsSubjectAndBody()
    {
        var template = CreateTemplate("Ämne", "Brödtext");

        Assert.Equal("Ämne", template.Subject);
        Assert.Equal("Brödtext", template.BodyMarkdown);
    }

    [Fact]
    public void Customize_UpdatesSubjectAndBody()
    {
        var template = CreateTemplate();

        template.Customize("Nytt ämne", "Ny brödtext");

        Assert.Equal("Nytt ämne", template.Subject);
        Assert.Equal("Ny brödtext", template.BodyMarkdown);
    }

    [Fact]
    public void Customize_SetsIsCustomizedToTrue()
    {
        var template = CreateTemplate();
        template.ResetToDefault("Standard", "Standard body");

        template.Customize("Anpassad", "Anpassad body");

        Assert.True(template.IsCustomized);
    }

    [Fact]
    public void Customize_WithEmptySubject_Throws()
    {
        var template = CreateTemplate();

        Assert.Throws<ArgumentException>(() => template.Customize("", "Body"));
    }

    [Fact]
    public void Customize_WithSubjectOver500Chars_Throws()
    {
        var template = CreateTemplate();
        var longSubject = new string('x', 501);

        Assert.Throws<ArgumentException>(() => template.Customize(longSubject, "Body"));
    }

    [Fact]
    public void Customize_WithBodyOver20000Chars_Throws()
    {
        var template = CreateTemplate();
        var longBody = new string('x', 20_001);

        Assert.Throws<ArgumentException>(() => template.Customize("Ämne", longBody));
    }

    [Fact]
    public void ResetToDefault_SetsIsCustomizedToFalse()
    {
        var template = CreateTemplate();

        template.ResetToDefault("Standard ämne", "Standard body");

        Assert.False(template.IsCustomized);
    }

    [Fact]
    public void ResetToDefault_ReplacesSubjectAndBody()
    {
        var template = CreateTemplate("Anpassat ämne", "Anpassad body");

        template.ResetToDefault("Standard ämne", "Standard body");

        Assert.Equal("Standard ämne", template.Subject);
        Assert.Equal("Standard body", template.BodyMarkdown);
    }

    [Fact]
    public void Customize_TrimsWhitespace()
    {
        var template = CreateTemplate();

        template.Customize("  Ämne  ", "  Body  ");

        Assert.Equal("Ämne", template.Subject);
        Assert.Equal("Body", template.BodyMarkdown);
    }

    private static MailTemplate CreateTemplate(string subject = "Testämne", string body = "Testbrödtext")
        => new(
            MailTemplateId.New(),
            ConventionId.New(),
            MailTemplateType.VisitorRegistrationConfirmed,
            subject,
            body);
}
