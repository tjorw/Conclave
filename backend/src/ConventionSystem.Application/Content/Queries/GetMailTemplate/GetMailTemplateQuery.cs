using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.GetMailTemplate;

public sealed record GetMailTemplateQuery(string TemplateType) : IQuery<MailTemplateDto?>;
