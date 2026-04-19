using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Tenancy.Ids;

namespace ConventionSystem.Domain.Tenancy.Events;

public record TenantCreated(
    TenantId TenantId,
    string Subdomain,
    string DisplayName,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TenantSuspended(
    TenantId TenantId,
    string Subdomain,
    DateTimeOffset OccurredAt) : IDomainEvent;

public record TenantRestored(
    TenantId TenantId,
    string Subdomain,
    DateTimeOffset OccurredAt) : IDomainEvent;