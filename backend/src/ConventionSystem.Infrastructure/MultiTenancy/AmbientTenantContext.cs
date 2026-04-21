using System.Threading;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public interface IAmbientTenantContext
{
    Guid? TenantId { get; }
    IDisposable Use(Guid tenantId);
}

public sealed class AmbientTenantContext : IAmbientTenantContext
{
    private static readonly AsyncLocal<Guid?> CurrentTenantId = new();

    public Guid? TenantId => CurrentTenantId.Value;

    public IDisposable Use(Guid tenantId)
    {
        var previousTenantId = CurrentTenantId.Value;
        CurrentTenantId.Value = tenantId;
        return new RestoreTenantScope(previousTenantId);
    }

    private sealed class RestoreTenantScope(Guid? previousTenantId) : IDisposable
    {
        public void Dispose()
        {
            CurrentTenantId.Value = previousTenantId;
        }
    }
}
