namespace ConventionSystem.Application.Common;

public interface IMailTemplateRenderer
{
    string RenderSubject(string template, IReadOnlyDictionary<string, string> variables);
    string RenderBody(string markdownTemplate, IReadOnlyDictionary<string, string> variables);
}
