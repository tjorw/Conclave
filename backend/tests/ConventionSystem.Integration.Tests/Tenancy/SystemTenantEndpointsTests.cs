using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ConventionSystem.Integration.Tests.Tenancy;

public sealed class SystemTenantEndpointsTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetSystemTenants_WithoutToken_Returns401()
    {
        var response = await Factory.CreateClient().GetAsync("/system/tenants");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSystemTenants_WithoutSystemAdminClaim_Returns403()
    {
        var token = CreateToken();
        var client = CreateAuthorizedClient(token);

        var response = await client.GetAsync("/system/tenants");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateSuspendRestoreTenant_AsSystemAdmin_Works()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);
        var subdomain = $"sys-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "System Tenant"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = createBody.GetProperty("id").GetGuid();

        var suspendResponse = await client.PutAsync($"/system/tenants/{tenantId}/suspend", content: null);
        Assert.Equal(HttpStatusCode.NoContent, suspendResponse.StatusCode);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var suspendedStatus = await db.Tenants
                .Where(t => t.Subdomain == subdomain)
                .Select(t => t.Status)
                .SingleAsync();
            Assert.Equal(TenantStatus.Suspended, suspendedStatus);
        }

        var restoreResponse = await client.PutAsync($"/system/tenants/{tenantId}/restore", content: null);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var activeStatus = await db.Tenants
                .Where(t => t.Subdomain == subdomain)
                .Select(t => t.Status)
                .SingleAsync();
            Assert.Equal(TenantStatus.Active, activeStatus);
        }
    }

    private static string CreateToken(params Claim[] extraClaims)
    {
        var claims = new List<Claim>
        {
            new("person_id", Guid.NewGuid().ToString())
        };
        claims.AddRange(extraClaims);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTimeOffset.UtcNow.AddHours(1).UtcDateTime,
            Issuer = "ConventionSystem",
            Audience = "ConventionSystem",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(ConventionSystemFactory.TestJwtKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private HttpClient CreateAuthorizedClient(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}