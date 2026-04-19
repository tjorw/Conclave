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
        var tenantAId = Guid.CreateVersion7();
        var tenantBId = Guid.CreateVersion7();
        var conventionId = Guid.CreateVersion7();

        // Setup: infoga en konvention direkt för tenant A via SQL för att kringgå interceptorn
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var name = "Tenant A Convention";
            var slug = "tenant-a-iso-test";
            await db.Database.ExecuteSqlAsync(
                $"INSERT INTO [conventions] ([Id], [Name], [Slug], [tenant_id]) VALUES ({conventionId}, {name}, {slug}, {tenantAId})");
        }

        // Tenant A ska se sin konvention
        await using var factoryA = CreateTenantFactory(tenantAId);
        await using (var scope = factoryA.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var conventions = await db.Conventions.ToListAsync();
            Assert.Contains(conventions, c => c.Id.Value == conventionId);
        }

        // Tenant B ska inte se tenant As konvention
        await using var factoryB = CreateTenantFactory(tenantBId);
        await using (var scope = factoryB.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var conventions = await db.Conventions.ToListAsync();
            Assert.DoesNotContain(conventions, c => c.Id.Value == conventionId);
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
