namespace ConventionSystem.Infrastructure.MultiTenancy;

public sealed class TenantContext : ITenantContext
{
    private Guid? _conventionId;
    private string? _connectionString;

    public bool IsResolved => _connectionString is not null;

    public Guid ConventionId => _conventionId
        ?? throw new InvalidOperationException("Tenant har inte lösts för denna request.");

    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("Tenant har inte lösts för denna request.");

    public void Resolve(Guid conventionId, string connectionString)
    {
        _conventionId = conventionId;
        _connectionString = connectionString;
    }
}
