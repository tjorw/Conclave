using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.UpdateMailTemplate;

public sealed record UpdateMailTemplateCommand(
    Guid ConventionId,
    string TemplateType,
    string Subject,
    string BodyMarkdown) : ICommand;
