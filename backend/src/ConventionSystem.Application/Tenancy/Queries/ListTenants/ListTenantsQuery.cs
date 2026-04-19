using ConventionSystem.Application.Common;
using ConventionSystem.Domain.Tenancy.Enums;

namespace ConventionSystem.Application.Tenancy.Queries.ListTenants;

public record ListTenantsQuery : IQuery<IReadOnlyList<TenantListItemDto>>;

public record TenantListItemDto(Guid Id, string Subdomain, string DisplayName, TenantStatus Status, DateTimeOffset CreatedAt);