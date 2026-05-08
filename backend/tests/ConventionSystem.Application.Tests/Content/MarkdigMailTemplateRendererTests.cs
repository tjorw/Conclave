using ConventionSystem.Infrastructure.Email;

namespace ConventionSystem.Application.Tests.Content;

public sealed class MarkdigMailTemplateRendererTests
{
    private readonly MarkdigMailTemplateRenderer _renderer = new();

    [Fact]
    public void RenderSubject_ReplacesKnownVariable()
    {
        var result = _renderer.RenderSubject("Hej {{firstName}}!", new Dictionary<string, string>
        {
            ["firstName"] = "Anna"
        });

        Assert.Equal("Hej Anna!", result);
    }

    [Fact]
    public void RenderSubject_UnknownVariable_ReplacesWithEmptyString()
    {
        var result = _renderer.RenderSubject("Hej {{okänd}}!", new Dictionary<string, string>());

        Assert.Equal("Hej !", result);
    }

    [Fact]
    public void RenderSubject_MultipleVariables_ReplacesAll()
    {
        var result = _renderer.RenderSubject("{{a}} och {{b}}", new Dictionary<string, string>
        {
            ["a"] = "Foo",
            ["b"] = "Bar"
        });

        Assert.Equal("Foo och Bar", result);
    }

    [Fact]
    public void RenderBody_RendersMarkdownToHtml()
    {
        var result = _renderer.RenderBody("**Fetstil**", new Dictionary<string, string>());

        Assert.Contains("<strong>Fetstil</strong>", result);
    }

    [Fact]
    public void RenderBody_SubstitutesBeforeRendering()
    {
        var result = _renderer.RenderBody("Hej **{{name}}**!", new Dictionary<string, string>
        {
            ["name"] = "Anna"
        });

        Assert.Contains("Anna", result);
        Assert.Contains("<strong>", result);
    }

    [Fact]
    public void RenderBody_DisablesRawHtml()
    {
        var result = _renderer.RenderBody("<script>alert('xss')</script>", new Dictionary<string, string>());

        Assert.DoesNotContain("<script>", result);
    }

    [Fact]
    public void RenderSubject_SameVariableTwice_BothReplaced()
    {
        var result = _renderer.RenderSubject("{{name}} hej {{name}}", new Dictionary<string, string>
        {
            ["name"] = "Anna"
        });

        Assert.Equal("Anna hej Anna", result);
    }
}
