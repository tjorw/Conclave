using ConventionSystem.Domain.Tenancy.Enums;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public sealed record ResolvedTenant(Guid Id, TenantStatus Status);
