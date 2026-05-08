using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Commands.ResetMailTemplate;

public sealed record ResetMailTemplateCommand(
    Guid ConventionId,
    string TemplateType) : ICommand;
