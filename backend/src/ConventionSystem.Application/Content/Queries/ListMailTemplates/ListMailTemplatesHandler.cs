using ConventionSystem.Application.Common;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Enums;

namespace ConventionSystem.Application.Content.Queries.ListMailTemplates;

public sealed class ListMailTemplatesHandler(
    IMailTemplateRepository mailTemplateRepository,
    IConventionRepository conventionRepository) : IRequestHandler<ListMailTemplatesQuery, IReadOnlyList<MailTemplateSummaryDto>>
{
    public async Task<IReadOnlyList<MailTemplateSummaryDto>> Handle(ListMailTemplatesQuery query, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct);
        if (convention is null)
            return [];

        var stored = new Dictionary<MailTemplateType, (bool IsCustomized, DateTimeOffset UpdatedAt)>();
        foreach (var type in DefaultMailTemplates.AllCustomizableTypes)
        {
            var template = await mailTemplateRepository.GetByTypeAsync(convention.Id, type, ct);
            if (template is not null)
                stored[type] = (template.IsCustomized, template.UpdatedAt);
        }

        return DefaultMailTemplates.AllCustomizableTypes
            .Select(type => stored.TryGetValue(type, out var info)
                ? new MailTemplateSummaryDto(type.ToString(), info.IsCustomized, info.UpdatedAt)
                : new MailTemplateSummaryDto(type.ToString(), false, null))
            .ToList();
    }
}
