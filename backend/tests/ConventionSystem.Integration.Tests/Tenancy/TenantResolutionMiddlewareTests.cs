using System.Net;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace ConventionSystem.Integration.Tests.Tenancy;

public sealed class TenantResolutionMiddlewareTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Request_WithUnknownSubdomain_Returns404TenantNotFound()
    {
        await using var multitenantFactory = CreateMultitenantFactory();
        var client = multitenantFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://unknown.conclave.se")
        });

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "nobody@test.se", password = "invalid" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("tenant_not_found", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Request_WithSuspendedTenant_Returns403TenantSuspended()
    {
        var subdomain = $"suspended-{Guid.NewGuid():N}";

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var tenant = new Tenant(TenantId.New(), subdomain, "Suspended Tenant");
            tenant.Suspend();

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = multitenantFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"http://{subdomain}.conclave.se")
        });

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "nobody@test.se", password = "invalid" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("tenant_suspended", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task DevelopmentRequest_WithoutSubdomain_UsesTenantHeaderFallback()
    {
        Guid tenantId;
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            tenantId = await db.Tenants
                .Select(t => t.Id.Value)
                .FirstAsync();
        }

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = multitenantFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
        client.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId.ToString());

        var response = await client.PostAsJsonAsync("/auth/login", new { email = "nobody@test.se", password = "invalid" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateMultitenantFactory()
    {
        return Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Multitenancy:Enabled"] = "true"
                });
            });
        });
    }
}