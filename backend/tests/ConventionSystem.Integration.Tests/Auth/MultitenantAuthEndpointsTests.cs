using System.IdentityModel.Tokens.Jwt;
using System.Net;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Domain.Tenancy.Ids;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConventionSystem.Integration.Tests.Auth;

public sealed class MultitenantAuthEndpointsTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Register_CreatesTenantUserAndLinkedPerson()
    {
        var (tenantId, subdomain) = await SeedTenantWithConventionAsync();
        await using var multitenantFactory = CreateMultitenantFactory();

        var registerClient = CreateClient(multitenantFactory, $"http://{subdomain}.conclave.se");
        var email = $"reg-{Guid.NewGuid():N}@test.se";

        var registerResponse = await registerClient.PostAsJsonAsync("/auth/register", new
        {
            email,
            password = "Test1234"
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();
        var conventionDb = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();

        var user = await identityDb.Users.SingleAsync(u => u.Email == email);
        Assert.Equal(UserType.TenantUser, user.UserType);
        Assert.Equal(tenantId, user.TenantId);
        Assert.NotNull(user.PersonId);

        var personIds = await conventionDb.Persons
            .Select(p => p.Id.Value)
            .ToListAsync();
        Assert.Contains(user.PersonId!.Value, personIds);
    }

    [Fact]
    public async Task Register_DuplicateEmailInSameTenant_Returns422WithErrorCode()
    {
        var (_, subdomain) = await SeedTenantWithConventionAsync();
        await using var multitenantFactory = CreateMultitenantFactory();

        var client = CreateClient(multitenantFactory, $"http://{subdomain}.conclave.se");
        var email = $"dup-{Guid.NewGuid():N}@test.se";

        var first = await client.PostAsJsonAsync("/auth/register", new { email, password = "Test1234" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/auth/register", new { email, password = "Test1234" });
        Assert.Equal((HttpStatusCode)422, second.StatusCode);

        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("email_already_exists", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Register_SameEmailDifferentTenants_IsAllowed()
    {
        var (_, subdomainA) = await SeedTenantWithConventionAsync();
        var (_, subdomainB) = await SeedTenantWithConventionAsync();
        await using var multitenantFactory = CreateMultitenantFactory();

        var email = $"shared-{Guid.NewGuid():N}@test.se";

        var clientA = CreateClient(multitenantFactory, $"http://{subdomainA}.conclave.se");
        var clientB = CreateClient(multitenantFactory, $"http://{subdomainB}.conclave.se");

        var responseA = await clientA.PostAsJsonAsync("/auth/register", new { email, password = "Test1234" });
        var responseB = await clientB.PostAsJsonAsync("/auth/register", new { email, password = "Test1234" });

        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
    }

    [Fact]
    public async Task TenantLogin_ReturnsJwtWithTenantIdAndTenantUserType()
    {
        var (tenantId, subdomain) = await SeedTenantWithConventionAsync();
        var email = $"tenant-{Guid.NewGuid():N}@test.se";
        const string password = "Test1234";

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateClient(multitenantFactory, $"http://{subdomain}.conclave.se");

        var register = await client.PostAsJsonAsync("/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        await ConfirmEmailAsync(email);

        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        var claims = ParseClaims(token).ToList();

        Assert.Contains(claims, c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
        Assert.Contains(claims, c => c.Type == "user_type" && c.Value == "tenant_user");
        Assert.DoesNotContain(claims, c => c.Type == "is_system_admin");
    }

    [Fact]
    public async Task TenantLogin_WithSystemAdminEmail_Returns401()
    {
        var (_, subdomain) = await SeedTenantWithConventionAsync();
        const string email = "sysadmin-login@test.se";
        const string password = "Test1234";
        await CreateSystemAdminUserAsync(email, password);

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateClient(multitenantFactory, $"http://{subdomain}.conclave.se");

        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SystemAdminLogin_ReturnsSystemAdminTokenWithoutTenantId()
    {
        const string email = "sysadmin-system@test.se";
        const string password = "Test1234";
        await CreateSystemAdminUserAsync(email, password);

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateClient(multitenantFactory, "http://system.conclave.se");

        var response = await client.PostAsJsonAsync("/system/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        var claims = ParseClaims(token).ToList();

        Assert.Contains(claims, c => c.Type == "user_type" && c.Value == "system_admin");
        Assert.Contains(claims, c => c.Type == "is_system_admin" && c.Value == "true");
        Assert.DoesNotContain(claims, c => c.Type == "tenant_id");
    }

    [Fact]
    public async Task SystemAdminLogin_WithTenantUserEmail_Returns401()
    {
        var (_, subdomain) = await SeedTenantWithConventionAsync();
        var email = $"tenant-only-{Guid.NewGuid():N}@test.se";
        const string password = "Test1234";

        await using var multitenantFactory = CreateMultitenantFactory();
        var tenantClient = CreateClient(multitenantFactory, $"http://{subdomain}.conclave.se");
        var register = await tenantClient.PostAsJsonAsync("/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        await ConfirmEmailAsync(email);

        var systemClient = CreateClient(multitenantFactory, "http://system.conclave.se");
        var response = await systemClient.PostAsJsonAsync("/system/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SystemAdminLogin_OnTenantSubdomain_Returns404()
    {
        var (_, subdomain) = await SeedTenantWithConventionAsync();
        const string email = "sysadmin-subdomain@test.se";
        const string password = "Test1234";
        await CreateSystemAdminUserAsync(email, password);

        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateClient(multitenantFactory, $"http://{subdomain}.conclave.se");

        var response = await client.PostAsJsonAsync("/system/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string baseAddress)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress)
        });
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

    private async Task<(Guid TenantId, string Subdomain)> SeedTenantWithConventionAsync()
    {
        var subdomain = $"mt-{Guid.NewGuid():N}";

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();

        var tenant = new Tenant(TenantId.New(), subdomain, $"Tenant {subdomain}");
        db.Tenants.Add(tenant);

        var convention = new Convention(ConventionId.New(), $"Convention {subdomain}", $"conv-{Guid.NewGuid():N}"[..20]);
        var conventionEntry = db.Conventions.Add(convention);
        conventionEntry.Property("TenantId").CurrentValue = tenant.Id.Value;

        await db.SaveChangesAsync();
        return (tenant.Id.Value, subdomain);
    }

    private async Task CreateSystemAdminUserAsync(string email, string password)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            UserType = UserType.SystemAdmin,
            EmailConfirmed = true,
            TenantId = null
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Kunde inte skapa systemadmin för test: {errors}");
        }
    }

    private async Task ConfirmEmailAsync(string email)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException("Kunde inte hitta användare för e-postbekräftelse i test.");
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
    }
}
