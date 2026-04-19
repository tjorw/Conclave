using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Data;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var adminEmail = $"sys-admin-{Guid.NewGuid():N}@test.se";

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "System Tenant",
            adminName = "System Admin",
            adminEmail,
            adminPassword = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = createBody.GetProperty("id").GetGuid();

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var tenantCreatedEventExists = await db.DomainEventLog
                .AnyAsync(e => e.EventType == "TenantCreated");

            Assert.True(tenantCreatedEventExists);
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

            var tenantRestoredEventExists = await db.DomainEventLog
                .AnyAsync(e => e.EventType == "TenantRestored");
            Assert.True(tenantRestoredEventExists);
        }

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
    }

    [Fact]
    public async Task CreateTenant_DuplicateSubdomain_Returns422()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);
        var subdomain = $"dup-{Guid.NewGuid():N}";
        var firstAdminEmail = $"dup-admin-1-{Guid.NewGuid():N}@test.se";
        var secondAdminEmail = $"dup-admin-2-{Guid.NewGuid():N}@test.se";

        var firstResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Tenant One",
            adminName = "Tenant One Admin",
            adminEmail = firstAdminEmail,
            adminPassword = "Admin123!"
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Tenant Two",
            adminName = "Tenant Two Admin",
            adminEmail = secondAdminEmail,
            adminPassword = "Admin123!"
        });

        Assert.Equal((HttpStatusCode)422, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task SuspendTenant_AlreadySuspended_Returns422()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);
        var subdomain = $"sus-{Guid.NewGuid():N}";
        var adminEmail = $"sus-admin-{Guid.NewGuid():N}@test.se";

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Suspend Tenant",
            adminName = "Suspend Admin",
            adminEmail,
            adminPassword = "Admin123!"
        });

        var tenantId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        var suspendResponse = await client.PutAsync($"/system/tenants/{tenantId}/suspend", content: null);
        Assert.Equal((HttpStatusCode)422, suspendResponse.StatusCode);
    }

    [Fact]
    public async Task RestoreTenant_AlreadyActive_Returns422()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);
        var subdomain = $"act-{Guid.NewGuid():N}";
        var adminEmail = $"act-admin-{Guid.NewGuid():N}@test.se";

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Active Tenant",
            adminName = "Active Admin",
            adminEmail,
            adminPassword = "Admin123!"
        });

        var tenantId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        var firstRestore = await client.PutAsync($"/system/tenants/{tenantId}/restore", content: null);
        Assert.Equal(HttpStatusCode.NoContent, firstRestore.StatusCode);

        var secondRestore = await client.PutAsync($"/system/tenants/{tenantId}/restore", content: null);
        Assert.Equal((HttpStatusCode)422, secondRestore.StatusCode);
    }

    [Fact]
    public async Task CreateTenant_AsSystemAdmin_CreatesConventionAndAdminUser_AndRequiresEmailConfirmationBeforeActivation()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        await using var multitenantFactory = CreateMultitenantFactory();
        var client = multitenantFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subdomain = $"tenant-{Guid.NewGuid():N}";
        var adminEmail = $"tenant-admin-{Guid.NewGuid():N}@test.se";
        var createTenantResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Provision Tenant",
            adminName = "Tenant Admin",
            adminEmail,
            adminPassword = "Admin123!"
        });
        Assert.Equal(HttpStatusCode.Created, createTenantResponse.StatusCode);

        var createBody = await createTenantResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = createBody.GetProperty("id").GetGuid();
        var conventionId = createBody.GetProperty("conventionId").GetGuid();
        Assert.NotEqual(Guid.Empty, conventionId);

        await using var scope = multitenantFactory.Services.CreateAsyncScope();
        var conventionDb = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var convention = await conventionDb.Conventions
            .Include(c => c.Administrators)
            .SingleAsync(c => c.Slug == subdomain);

        Assert.Equal("Provision Tenant", convention.Name);
        Assert.Equal(conventionId, convention.Id.Value);
        Assert.NotEmpty(convention.Administrators);

        var tenantStatusBeforeConfirmation = await conventionDb.Tenants
            .Where(t => t.Subdomain == subdomain)
            .Select(t => t.Status)
            .SingleAsync();
        Assert.Equal(TenantStatus.Suspended, tenantStatusBeforeConfirmation);

        var user = await identityDb.Users.SingleAsync(u => u.Email == adminEmail);
        Assert.Equal(UserType.TenantUser, user.UserType);
        Assert.Equal(tenantId, user.TenantId);
        Assert.NotNull(user.PersonId);
        Assert.False(user.EmailConfirmed);
        Assert.Contains(convention.Administrators, a => a.PersonId.Value == user.PersonId!.Value);

        var loginClient = CreateTenantClient(multitenantFactory, $"http://{subdomain}.conclave.se");
        var blockedLoginResponse = await loginClient.PostAsJsonAsync("/auth/login", new
        {
            email = adminEmail,
            password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, blockedLoginResponse.StatusCode);

        var confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmResponse = await client.PostAsJsonAsync("/auth/confirm-email", new
        {
            email = adminEmail,
            token = confirmToken
        });
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var tenantStatusAfterConfirmation = await conventionDb.Tenants
            .Where(t => t.Subdomain == subdomain)
            .Select(t => t.Status)
            .SingleAsync();
        Assert.Equal(TenantStatus.Active, tenantStatusAfterConfirmation);

        var userAfterConfirmation = await identityDb.Users
            .AsNoTracking()
            .SingleAsync(u => u.Email == adminEmail);
        Assert.True(userAfterConfirmation.EmailConfirmed);

        var loginResponse = await loginClient.PostAsJsonAsync("/auth/login", new
        {
            email = adminEmail,
            password = "Admin123!"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var jwtToken = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("token").GetString()!;
        var claims = ParseClaims(jwtToken).ToList();
        Assert.Contains(claims, c => c.Type == "is_admin" && c.Value == "true");
    }

    [Fact]
    public async Task CreateTenant_BootstrapAdminCanLoginViaTenantSubdomain_JwtContainsIsAdminClaim()
    {
        var sysToken = CreateToken(new Claim("is_system_admin", "true"));
        var sysClient = CreateAuthorizedClient(sysToken);

        var subdomain = $"portal-{Guid.NewGuid():N}";
        var bootstrapAdminEmail = $"bootstrap-admin-{Guid.NewGuid():N}@test.se";
        const string bootstrapAdminPassword = "Admin123!";
        var createResponse = await sysClient.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Portal Test Tenant",
            adminName = "Bootstrap Admin",
            adminEmail = bootstrapAdminEmail,
            adminPassword = bootstrapAdminPassword
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var tenantId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var restoreResponse = await sysClient.PutAsync($"/system/tenants/{tenantId}/restore", content: null);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var bootstrapUser = await userManager.FindByEmailAsync(bootstrapAdminEmail);
            Assert.NotNull(bootstrapUser);

            var confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(bootstrapUser!);
            var confirmResponse = await sysClient.PostAsJsonAsync("/auth/confirm-email", new
            {
                email = bootstrapAdminEmail,
                token = confirmToken
            });

            Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        }

        await using var multitenantFactory = CreateMultitenantFactory();
        var tenantClient = CreateTenantClient(multitenantFactory, $"http://{subdomain}.conclave.se");

        var loginResponse = await tenantClient.PostAsJsonAsync("/auth/login", new
        {
            email = bootstrapAdminEmail,
            password = bootstrapAdminPassword
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var jwtToken = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("token").GetString()!;
        var claims = ParseClaims(jwtToken).ToList();

        Assert.Contains(claims, c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
        Assert.Contains(claims, c => c.Type == "user_type" && c.Value == "tenant_user");
        Assert.Contains(claims, c => c.Type == "is_admin" && c.Value == "true");
    }

    [Fact]
    public async Task ProvisionTenantConvention_UnknownTenant_Returns404()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);
        var bootstrapAdminEmail = $"unknown-tenant-bootstrap-{Guid.NewGuid():N}@test.se";

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain = $"unknown-test-{Guid.NewGuid():N}",
            displayName = "Unknown Test Tenant",
            adminName = "Unknown Bootstrap",
            adminEmail = bootstrapAdminEmail,
            adminPassword = "Admin123!"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await client.PostAsJsonAsync($"/system/tenants/{Guid.NewGuid()}/provision", new
        {
            conventionName = "Unknown Tenant Convention",
            conventionSlug = "unknown-tenant-convention",
            adminName = "Tenant Admin",
            adminEmail = "unknown-tenant-admin@test.se",
            adminPassword = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private static HttpClient CreateTenantClient(WebApplicationFactory<Program> factory, string baseAddress) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress)
        });

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