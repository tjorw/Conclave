using ConventionSystem.Domain.Tenancy.Enums;
using MediatR;

namespace ConventionSystem.Application.Tenancy.Queries.ListTenants;

public record ListTenantsQuery : IRequest<IReadOnlyList<TenantListItemDto>>;

public record TenantListItemDto(Guid Id, string Subdomain, string DisplayName, TenantStatus Status, DateTimeOffset CreatedAt);