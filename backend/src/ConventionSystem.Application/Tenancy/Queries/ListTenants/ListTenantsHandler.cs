using ConventionSystem.Application.Tenancy.Abstractions;

namespace ConventionSystem.Application.Tenancy.Queries.ListTenants;

public sealed class ListTenantsHandler(ITenantRepository repository)
    : IRequestHandler<ListTenantsQuery, IReadOnlyList<TenantListItemDto>>
{
    public Task<IReadOnlyList<TenantListItemDto>> Handle(ListTenantsQuery request, CancellationToken ct)
        => repository.ListAsync(ct);
}