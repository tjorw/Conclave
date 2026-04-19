using ConventionSystem.Domain.Common;

namespace ConventionSystem.Domain.Tenancy.Exceptions;

public sealed class TenantAlreadySuspendedException()
    : DomainRuleViolationException(
        "Tenanten är redan suspenderad.",
        "tenant_already_suspended");

public sealed class TenantAlreadyActiveException()
    : DomainRuleViolationException(
        "Tenanten är redan aktiv.",
        "tenant_already_active");