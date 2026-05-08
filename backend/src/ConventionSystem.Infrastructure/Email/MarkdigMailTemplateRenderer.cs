using System.Text.RegularExpressions;
using ConventionSystem.Application.Common;
using Markdig;

namespace ConventionSystem.Infrastructure.Email;

public sealed class MarkdigMailTemplateRenderer : IMailTemplateRenderer
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public string RenderSubject(string template, IReadOnlyDictionary<string, string> variables)
        => Substitute(template, variables);

    public string RenderBody(string markdownTemplate, IReadOnlyDictionary<string, string> variables)
    {
        var substituted = Substitute(markdownTemplate, variables);
        return Markdown.ToHtml(substituted, Pipeline);
    }

    private static string Substitute(string template, IReadOnlyDictionary<string, string> variables)
        => PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : string.Empty;
        });
}
