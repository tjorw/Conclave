using ConventionSystem.Infrastructure.MultiTenancy;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConventionSystem.Integration.Tests.Tenancy;

public sealed class TenantDataIsolationTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ConventionDbContext_QueryFilter_IsolatesDataBetweenTenants()
    {
        // Den seedade konventionen skapades utan multitenansflag → tenant_id = Guid.Empty.
        // Vi använder Guid.Empty som "tenant A" för att undvika att sätta in extra testdata
        // i den delade databasen (vilket stör andra tests GetSingleAsync-anrop).
        var tenantAId = Guid.Empty;
        var tenantBId = Guid.CreateVersion7();

        // Tenant A ska se den seedade konventionen
        await using var factoryA = CreateTenantFactory(tenantAId);
        await using (var scope = factoryA.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var conventions = await db.Conventions.ToListAsync();
            Assert.NotEmpty(conventions);
        }

        // Tenant B ska inte se tenant As konvention
        await using var factoryB = CreateTenantFactory(tenantBId);
        await using (var scope = factoryB.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var conventions = await db.Conventions.ToListAsync();
            Assert.Empty(conventions);
        }
    }

    private WebApplicationFactory<Program> CreateTenantFactory(Guid tenantId) =>
        Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Multitenancy:Enabled"] = "true"
                }));
            builder.ConfigureServices(services =>
                services.AddScoped<ITenantContext>(_ => new FixedTenantContext(tenantId)));
        });

    private sealed class FixedTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
    }
}
