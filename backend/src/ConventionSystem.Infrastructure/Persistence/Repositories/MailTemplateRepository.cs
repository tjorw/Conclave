using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class MailTemplateRepository(ConventionDbContext db) : IMailTemplateRepository
{
    public Task<MailTemplate?> GetByTypeAsync(ConventionId conventionId, MailTemplateType templateType, CancellationToken ct = default)
        => db.MailTemplates
            .FirstOrDefaultAsync(t => t.ConventionId == conventionId && t.TemplateType == templateType, ct);

    public async Task AddAsync(MailTemplate template, CancellationToken ct = default)
        => await db.MailTemplates.AddAsync(template, ct);

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
