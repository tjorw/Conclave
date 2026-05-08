using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Content.Abstractions;

public interface IMailTemplateRepository
{
    Task<MailTemplate?> GetByTypeAsync(ConventionId conventionId, MailTemplateType templateType, CancellationToken ct = default);
    Task AddAsync(MailTemplate template, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
