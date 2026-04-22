using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace ConventionSystem.Integration.Tests.Tenancy;

/// <summary>
/// Verifierar att en inloggad användare inte kan byta subdomän och
/// av misstag få tillgång till en annan tenants data.
///
/// Angreppsscenario: Alice loggar in på tenant A och får en JWT med
/// tenant_id = A. Hon skickar sedan requester till tenant B:s endpoint
/// med samma token. Systemet ska avvisa detta med 401 tenant_mismatch.
/// </summary>
public sealed class CrossTenantSecurityTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Request_WithTokenFromDifferentTenant_Returns401WithTenantMismatch()
    {
        var tenantAId = Guid.CreateVersion7();
        var tenantBId = await SeedActiveTenantAsync();

        var tokenForTenantA = CreateTenantToken(tenantAId);

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateTenantClient(multitenantFactory, tenantBId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenForTenantA);

        var response = await client.GetAsync("/me/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("tenant_mismatch", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Request_WithAdminTokenFromDifferentTenant_Returns401BeforeAdminCheckCanPass()
    {
        // Admin-token från tenant A ska inte ge admin-access i tenant B –
        // tenant_mismatch ska slå till i middleware innan endpoints/policies evalueras.
        var tenantAId = Guid.CreateVersion7();
        var tenantBId = await SeedActiveTenantAsync();

        var adminTokenForTenantA = CreateTenantToken(tenantAId, isAdmin: true);

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateTenantClient(multitenantFactory, tenantBId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokenForTenantA);

        var response = await client.GetAsync("/me/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("tenant_mismatch", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Request_WithTokenFromCorrectTenant_IsNotRejectedForTenantMismatch()
    {
        var tenantId = await SeedActiveTenantAsync();
        var tokenForTenant = CreateTenantToken(tenantId);

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateTenantClient(multitenantFactory, tenantId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenForTenant);

        var response = await client.GetAsync("/me/profile");

        // Tenant-kontrollen passerar. Personen finns inte → 404.
        // Det viktiga är att vi inte får 401 pga tenant_mismatch.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithoutToken_Returns401FromAuthNotFromTenantMismatch()
    {
        var tenantId = await SeedActiveTenantAsync();

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateTenantClient(multitenantFactory, tenantId);
        // Ingen Authorization-header

        var response = await client.GetAsync("/me/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Responsen ska inte innehålla tenant_mismatch – felet kommer från
        // standard-autentiseringen, inte från vår tenant-kontroll.
        var content = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                var body = JsonSerializer.Deserialize<JsonElement>(content);
                if (body.TryGetProperty("errorCode", out var code))
                    Assert.NotEqual("tenant_mismatch", code.GetString());
            }
            catch (JsonException)
            {
                // Icke-JSON-svar är OK – det är standard 401-challenge, inte vår ProblemDetails
            }
        }
    }

    [Fact]
    public async Task CrossTenantRequest_CannotReadAnotherTenantsData_EvenWithValidToken()
    {
        // Säkerställer att query-filter + tenant-mismatch-kontrollen tillsammans
        // gör att data från tenant B aldrig exponeras för en tenant A-token.
        var tenantAId = Guid.CreateVersion7();
        var tenantBId = await SeedActiveTenantAsync();

        var tokenForTenantA = CreateTenantToken(tenantAId);

        await using var multitenantFactory = CreateMultitenantFactory();

        // Direktanrop mot tenant B med token från tenant A
        var clientToTenantB = CreateTenantClient(multitenantFactory, tenantBId);
        clientToTenantB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenForTenantA);

        var meResponse = await clientToTenantB.GetAsync("/me/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        // Kontrollera att felet är tenant_mismatch och inte ett dataläckage
        var body = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("tenant_mismatch", body.GetProperty("errorCode").GetString());
    }

    // ─── Hjälpmetoder ────────────────────────────────────────────────────────

    private async Task<Guid> SeedActiveTenantAsync()
    {
        var subdomain = $"sec-{Guid.NewGuid():N}"[..30];
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var tenant = new Tenant(TenantId.New(), subdomain, $"Security-test tenant {subdomain}");
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id.Value;
    }

    private static HttpClient CreateTenantClient(WebApplicationFactory<Program> factory, Guid tenantId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
        client.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId.ToString());
        return client;
    }

    private static string CreateTenantToken(Guid tenantId, bool isAdmin = false)
    {
        var claims = new List<Claim>
        {
            new("person_id", Guid.NewGuid().ToString()),
            new("tenant_id", tenantId.ToString()),
            new("user_type", "tenant_user")
        };
        if (isAdmin)
            claims.Add(new("is_admin", "true"));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTimeOffset.UtcNow.AddHours(1).UtcDateTime,
            Issuer = ConventionSystemFactory.TestJwtIssuer,
            Audience = ConventionSystemFactory.TestJwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(ConventionSystemFactory.TestJwtKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
    }

    private WebApplicationFactory<Program> CreateMultitenantFactory() =>
        Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Multitenancy:Enabled"] = "true"
                }));
        });
}
