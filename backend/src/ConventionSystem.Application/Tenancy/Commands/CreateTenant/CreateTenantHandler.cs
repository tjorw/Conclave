using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Ids;
using MediatR;

namespace ConventionSystem.Application.Tenancy.Commands.CreateTenant;

public sealed class CreateTenantHandler(ITenantRepository repository) : IRequestHandler<CreateTenantCommand, Guid>
{
    public async Task<Guid> Handle(CreateTenantCommand command, CancellationToken ct)
    {
        var normalizedSubdomain = command.Subdomain.Trim().ToLowerInvariant();
        if (await repository.SubdomainExistsAsync(normalizedSubdomain, ct))
            throw new InvalidOperationException($"Subdomän '{normalizedSubdomain}' används redan.");

        var tenant = new Tenant(TenantId.New(), normalizedSubdomain, command.DisplayName);
        tenant.Suspend();

        await repository.AddAsync(tenant, ct);
        await repository.SaveAsync(ct);

        return tenant.Id.Value;
    }
}