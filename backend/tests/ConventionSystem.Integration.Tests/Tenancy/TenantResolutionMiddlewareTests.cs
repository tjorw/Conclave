using System.Net;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace ConventionSystem.Integration.Tests.Tenancy;

public sealed class TenantResolutionMiddlewareTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task SystemAuthLogin_WithoutTenantSignal_BypassesTenantResolution()
    {
        await using var multitenantFactory = CreateMultitenantFactory();
        var client = multitenantFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        var response = await client.PostAsJsonAsync("/system/auth/login", new { email = "nobody@test.se", password = "invalid" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SystemSignup_WithoutTenantSignal_BypassesTenantResolution()
    {
        await using var multitenantFactory = CreateMultitenantFactory();
        var client = multitenantFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
        var subdomain = $"bypass-{Guid.NewGuid():N}"[..20];

        var response = await client.PostAsJsonAsync("/system/signup", new
        {
            organizationName = "Bypass Tenant",
            subdomain,
            contactName = "Bypass Owner",
            contactEmail = $"bypass-{Guid.NewGuid():N}@test.se"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedSystemTenants_WithoutTenantSignal_BypassesTenantResolution()
    {
        await using var multitenantFactory = CreateMultitenantFactory();
        var client = multitenantFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        var response = await client.GetAsync("/system/tenants");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

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
                .Where(t => t.Status == TenantStatus.Active)
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

    [Fact]
    public async Task Request_AfterSuspend_UsesInvalidatedCacheAndReturns403()
    {
        var subdomain = $"cache-suspend-{Guid.NewGuid():N}";

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            db.Tenants.Add(new Tenant(TenantId.New(), subdomain, "Cache Suspend Tenant"));
            await db.SaveChangesAsync();
        }

        await using var multitenantFactory = CreateMultitenantFactory();

        var client = CreateSystemAdminClient(multitenantFactory, $"http://{subdomain}.conclave.se");

        var beforeSuspend = await client.PostAsJsonAsync("/auth/login", new { email = "nobody@test.se", password = "invalid" });
        Assert.Equal(HttpStatusCode.Unauthorized, beforeSuspend.StatusCode);

        await using var queryScope = Factory.Services.CreateAsyncScope();
        var queryDb = queryScope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var tenantId = await queryDb.Tenants.Where(t => t.Subdomain == subdomain).Select(t => t.Id.Value).SingleAsync();

        var suspendResponse = await client.PutAsync($"/system/tenants/{tenantId}/suspend", content: null);
        Assert.Equal(HttpStatusCode.NoContent, suspendResponse.StatusCode);

        var afterSuspend = await client.PostAsJsonAsync("/auth/login", new { email = "nobody@test.se", password = "invalid" });
        Assert.Equal(HttpStatusCode.Forbidden, afterSuspend.StatusCode);
    }

    [Fact]
    public async Task Request_AfterRestore_UsesInvalidatedCacheAndReturns401()
    {
        var subdomain = $"cache-restore-{Guid.NewGuid():N}";
        var contextSubdomain = $"cache-context-{Guid.NewGuid():N}";
        Guid contextTenantId;

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var tenant = new Tenant(TenantId.New(), subdomain, "Cache Restore Tenant");
            tenant.Suspend();
            db.Tenants.Add(tenant);
            var contextTenant = new Tenant(TenantId.New(), contextSubdomain, "Cache Context Tenant");
            db.Tenants.Add(contextTenant);
            await db.SaveChangesAsync();
            contextTenantId = contextTenant.Id.Value;
        }

        await using var multitenantFactory = CreateMultitenantFactory();

        var tenantClient = multitenantFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"http://{subdomain}.conclave.se")
        });

        var systemClient = CreateSystemAdminClient(multitenantFactory, "http://localhost");
        systemClient.DefaultRequestHeaders.Add("X-Tenant-ID", contextTenantId.ToString());

        var beforeRestore = await tenantClient.PostAsJsonAsync("/auth/login", new { email = "nobody@test.se", password = "invalid" });
        Assert.Equal(HttpStatusCode.Forbidden, beforeRestore.StatusCode);

        await using var queryScope = Factory.Services.CreateAsyncScope();
        var queryDb = queryScope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var tenantId = await queryDb.Tenants.Where(t => t.Subdomain == subdomain).Select(t => t.Id.Value).SingleAsync();

        var restoreResponse = await systemClient.PutAsync($"/system/tenants/{tenantId}/restore", content: null);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        var afterRestore = await tenantClient.PostAsJsonAsync("/auth/login", new { email = "nobody@test.se", password = "invalid" });
        Assert.Equal(HttpStatusCode.Unauthorized, afterRestore.StatusCode);
    }

    private static HttpClient CreateSystemAdminClient(WebApplicationFactory<Program> factory, string baseAddress)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress)
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateSystemAdminToken());
        return client;
    }

    private static string CreateSystemAdminToken()
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("person_id", Guid.NewGuid().ToString()),
                new Claim("is_system_admin", "true")
            ]),
            Expires = DateTimeOffset.UtcNow.AddHours(1).UtcDateTime,
            Issuer = ConventionSystemFactory.TestJwtIssuer,
            Audience = ConventionSystemFactory.TestJwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(ConventionSystemFactory.TestJwtKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
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
