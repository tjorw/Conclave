using ConventionSystem.Application.Common;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Enums;

namespace ConventionSystem.Application.Content.Queries.GetMailTemplate;

public sealed class GetMailTemplateHandler(
    IMailTemplateRepository mailTemplateRepository,
    IConventionRepository conventionRepository) : IRequestHandler<GetMailTemplateQuery, MailTemplateDto?>
{
    public async Task<MailTemplateDto?> Handle(GetMailTemplateQuery query, CancellationToken ct)
    {
        if (!Enum.TryParse<MailTemplateType>(query.TemplateType, ignoreCase: true, out var templateType))
            return null;

        var convention = await conventionRepository.GetSingleAsync(ct);
        if (convention is null)
            return null;

        var template = await mailTemplateRepository.GetByTypeAsync(convention.Id, templateType, ct);
        var (defaultSubject, defaultBody) = DefaultMailTemplates.GetTemplate(templateType);

        return new MailTemplateDto(
            templateType.ToString(),
            template?.Subject ?? defaultSubject,
            template?.BodyMarkdown ?? defaultBody,
            template?.IsCustomized ?? false,
            template?.UpdatedAt,
            MailTemplateVariables.GetVariables(templateType));
    }
}
