using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Enums;

namespace ConventionSystem.Application.Content.Commands.ResetMailTemplate;

public sealed class ResetMailTemplateHandler(
    IMailTemplateRepository mailTemplateRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<ResetMailTemplateCommand>
{
    protected override async Task ExecuteAsync(ResetMailTemplateCommand command, CancellationToken ct)
    {
        if (!Enum.TryParse<MailTemplateType>(command.TemplateType, ignoreCase: true, out var templateType))
            throw new ArgumentException($"Okänd malltyp: {command.TemplateType}.");

        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan återställa mailmallar.");

        var template = await mailTemplateRepository.GetByTypeAsync(convention.Id, templateType, ct);
        if (template is null)
            return;

        var (defaultSubject, defaultBody) = DefaultMailTemplates.GetTemplate(templateType);
        template.ResetToDefault(defaultSubject, defaultBody);

        await mailTemplateRepository.SaveAsync(ct);
    }
}
