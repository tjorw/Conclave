using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.ListMailTemplates;

public sealed record ListMailTemplatesQuery : IQuery<IReadOnlyList<MailTemplateSummaryDto>>;
