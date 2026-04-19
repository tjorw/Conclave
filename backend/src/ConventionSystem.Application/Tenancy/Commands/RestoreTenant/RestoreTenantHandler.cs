using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Domain.Tenancy.Ids;
using MediatR;

namespace ConventionSystem.Application.Tenancy.Commands.RestoreTenant;

public sealed class RestoreTenantHandler(ITenantRepository repository) : IRequestHandler<RestoreTenantCommand>
{
    public async Task Handle(RestoreTenantCommand command, CancellationToken ct)
    {
        var tenantId = new TenantId(command.TenantId);
        var tenant = await repository.GetByIdAsync(tenantId, ct)
            ?? throw new ResourceNotFoundException("Tenant", command.TenantId.ToString());

        tenant.Restore();
        await repository.SaveAsync(ct);
    }
}