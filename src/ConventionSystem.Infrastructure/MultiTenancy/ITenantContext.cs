namespace ConventionSystem.Infrastructure.MultiTenancy;

public interface ITenantContext
{
    bool IsResolved { get; }
    Guid ConventionId { get; }
    string ConnectionString { get; }
}
