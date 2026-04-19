using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Domain.Tenancy.Ids;
using MediatR;

namespace ConventionSystem.Application.Tenancy.Commands.SuspendTenant;

public sealed class SuspendTenantHandler(ITenantRepository repository) : IRequestHandler<SuspendTenantCommand>
{
    public async Task Handle(SuspendTenantCommand command, CancellationToken ct)
    {
        var tenantId = new TenantId(command.TenantId);
        var tenant = await repository.GetByIdAsync(tenantId, ct)
            ?? throw new ResourceNotFoundException("Tenant", command.TenantId.ToString());

        tenant.Suspend();
        await repository.SaveAsync(ct);
    }
}