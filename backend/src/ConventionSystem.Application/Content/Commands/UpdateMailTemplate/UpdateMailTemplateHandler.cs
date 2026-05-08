using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Content.Commands.UpdateMailTemplate;

public sealed class UpdateMailTemplateHandler(
    IMailTemplateRepository mailTemplateRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<UpdateMailTemplateCommand>
{
    protected override async Task ExecuteAsync(UpdateMailTemplateCommand command, CancellationToken ct)
    {
        if (!Enum.TryParse<MailTemplateType>(command.TemplateType, ignoreCase: true, out var templateType))
            throw new ArgumentException($"Okänd malltyp: {command.TemplateType}.");

        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan redigera mailmallar.");

        var template = await mailTemplateRepository.GetByTypeAsync(convention.Id, templateType, ct);

        if (template is null)
        {
            template = new MailTemplate(MailTemplateId.New(), convention.Id, templateType, command.Subject, command.BodyMarkdown);
            await mailTemplateRepository.AddAsync(template, ct);
        }
        else
        {
            template.Customize(command.Subject, command.BodyMarkdown);
        }

        await mailTemplateRepository.SaveAsync(ct);
    }
}
