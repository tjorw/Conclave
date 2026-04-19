using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Infrastructure.MultiTenancy;

public interface ITenantContext
{
    Guid TenantId { get; }
}

public sealed class DefaultTenantContext(
    IConfiguration configuration,
    IOptions<MultitenancyOptions> options) : ITenantContext
{
    private Guid? _tenantId;

    public Guid TenantId => _tenantId ??= ResolveTenantId(configuration, options.Value);

    private static Guid ResolveTenantId(IConfiguration configuration, MultitenancyOptions options)
    {
        if (!options.Enabled)
            return Guid.Empty;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Anslutningsstrang saknas for DefaultConnection.");

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var bySubdomain = connection.CreateCommand();
        bySubdomain.CommandText =
            "SELECT TOP (1) [Id] FROM [tenants] WHERE [Subdomain] = @subdomain ORDER BY [created_at]";
        bySubdomain.Parameters.AddWithValue("@subdomain", options.DefaultSubdomain);

        var bySubdomainResult = bySubdomain.ExecuteScalar();
        if (bySubdomainResult is Guid tenantIdBySubdomain)
            return tenantIdBySubdomain;

        using var firstTenant = connection.CreateCommand();
        firstTenant.CommandText = "SELECT TOP (1) [Id] FROM [tenants] ORDER BY [created_at]";

        var firstTenantResult = firstTenant.ExecuteScalar();
        if (firstTenantResult is Guid firstTenantId)
            return firstTenantId;

        throw new InvalidOperationException("Ingen tenant hittades i databasen.");
    }
}