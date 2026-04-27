using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Data;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "System Tenant"
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
    public async Task CreateTenant_AsSystemAdmin_ReturnsTenantId_AndTenantIsSuspended()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);
        var subdomain = $"new-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "New Tenant"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = createBody.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, tenantId);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var status = await db.Tenants
            .Where(t => t.Subdomain == subdomain)
            .Select(t => t.Status)
            .SingleAsync();
        Assert.Equal(TenantStatus.Suspended, status);
    }

    [Fact]
    public async Task CreateTenant_DuplicateSubdomain_Returns422()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);
        var subdomain = $"dup-{Guid.NewGuid():N}";

        var firstResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Tenant One"
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Tenant Two"
        });

        Assert.Equal((HttpStatusCode)422, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task SuspendTenant_AlreadySuspended_Returns422()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);
        var subdomain = $"sus-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Suspend Tenant"
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

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Active Tenant"
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
    public async Task ProvisionTenantConvention_UnknownTenant_Returns404()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);

        var response = await client.PostAsJsonAsync($"/system/tenants/{Guid.NewGuid()}/provision", new
        {
            adminName = "Tenant Admin",
            adminEmail = "unknown-tenant-admin@test.se",
            adminPassword = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProvisionTenantConvention_SendsProvisioningWelcomeEmail()
    {
        var fakeEmailService = new CapturingEmailService();
        await using var app = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["App:AdminUrlTemplate"] = "https://{subdomain}.conclave.se/admin"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(fakeEmailService);
            });
        });

        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subdomain = $"mail-{Guid.NewGuid():N}";
        var adminEmail = $"mail-admin-{Guid.NewGuid():N}@test.se";

        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Mail Tenant"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var tenantId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        var restoreResponse = await client.PutAsync($"/system/tenants/{tenantId}/restore", content: null);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        var provisionResponse = await client.PostAsJsonAsync($"/system/tenants/{tenantId}/provision", new
        {
            adminName = "Mail Admin",
            adminEmail,
            adminPassword = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.Created, provisionResponse.StatusCode);

        Assert.Single(fakeEmailService.ProvisionedWelcomeEmails);
        var email = fakeEmailService.ProvisionedWelcomeEmails.Single();
        Assert.Equal(adminEmail, email.ToEmail);
        Assert.Equal("Mail Admin", email.ToName);
        Assert.Equal("Mail Tenant", email.OrganizationName);
        Assert.Equal(subdomain, email.Subdomain);
        Assert.Equal("Admin123!", email.TemporaryPassword);
        Assert.Equal($"https://{subdomain}.conclave.se/admin/login", email.LoginLink);
    }

    [Fact]
    public async Task PublicSignup_CreatesSuspendedTenant_Convention_AndPendingAdmin()
    {
        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateTenantClient(multitenantFactory, "http://system.conclave.se");
        var subdomain = $"signup-{Guid.NewGuid():N}"[..20];
        var email = $"signup-{Guid.NewGuid():N}@test.se";

        var response = await client.PostAsJsonAsync("/system/signup", new
        {
            organizationName = "Signup Tenant",
            subdomain,
            contactName = "Signup Owner",
            contactEmail = email
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = body.GetProperty("tenantId").GetGuid();
        var conventionId = body.GetProperty("conventionId").GetGuid();

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var tenant = await db.Tenants.SingleAsync(t => t.Subdomain == subdomain);
        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        Assert.Equal(subdomain, tenant.Subdomain);

        var conventionTenantId = await db.Conventions
            .Where(c => c.Id == new ConventionId(conventionId))
            .Select(c => EF.Property<Guid>(c, "TenantId"))
            .SingleAsync();
        Assert.Equal(tenantId, conventionTenantId);

        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
        Assert.Equal(tenantId, user.TenantId);
        Assert.NotNull(user.PersonId);

        var claims = await userManager.GetClaimsAsync(user);
        Assert.Contains(claims, c => c.Type == "activates_tenant" && c.Value == "true");
    }

    [Fact]
    public async Task PublicSignup_ConfirmEmail_ActivatesTenant()
    {
        await using var multitenantFactory = CreateMultitenantFactory();
        var client = CreateTenantClient(multitenantFactory, "http://system.conclave.se");
        var subdomain = $"confirm-{Guid.NewGuid():N}"[..20];
        var email = $"confirm-{Guid.NewGuid():N}@test.se";

        var signupResponse = await client.PostAsJsonAsync("/system/signup", new
        {
            organizationName = "Confirm Tenant",
            subdomain,
            contactName = "Confirm Owner",
            contactEmail = email
        });

        Assert.Equal(HttpStatusCode.Created, signupResponse.StatusCode);
        var signupBody = await signupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = signupBody.GetProperty("tenantId").GetGuid();

        string token;
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
        }

        var confirmResponse = await client.PostAsJsonAsync("/auth/confirm-email", new
        {
            email,
            token,
            tenantId
        });

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var tenant = await db.Tenants.SingleAsync(t => t.Subdomain == subdomain);
            Assert.Equal(TenantStatus.Active, tenant.Status);

            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True(user!.EmailConfirmed);

            var claims = await userManager.GetClaimsAsync(user);
            Assert.DoesNotContain(claims, c => c.Type == "activates_tenant" && c.Value == "true");
        }
    }

    [Fact]
    public async Task SystemTenantConventionAdmins_CanBeManagedFromSystemEndpoints()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);

        var subdomain = $"admins-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Admins Tenant"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var tenantId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var provisionResponse = await client.PostAsJsonAsync($"/system/tenants/{tenantId}/provision", new
        {
            adminName = "Bootstrap Admin",
            adminEmail = $"admins-bootstrap-{Guid.NewGuid():N}@test.se",
            adminPassword = "Admin123!"
        });
        Assert.Equal(HttpStatusCode.Created, provisionResponse.StatusCode);
        var conventionId = (await provisionResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("conventionId").GetGuid();

        Guid personId;
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var conventionRepository = scope.ServiceProvider.GetRequiredService<IConventionRepository>();
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();

            var convention = await conventionRepository.GetByIdAsync(new ConventionId(conventionId));
            Assert.NotNull(convention);

            var person = convention!.CreatePerson("Portal Admin Candidate", $"candidate-{Guid.NewGuid():N}@test.se");
            await db.Persons.AddAsync(person);
            db.Entry(person).Property("TenantId").CurrentValue = tenantId;
            await db.SaveChangesAsync();
            personId = person.Id.Value;

            var personTenantId = await db.Persons
                .Where(p => p.Id == new PersonId(personId))
                .Select(p => EF.Property<Guid>(p, "TenantId"))
                .SingleAsync();
            Assert.Equal(tenantId, personTenantId);
        }

        var conventionsResponse = await client.GetAsync($"/system/tenants/{tenantId}/conventions");
        Assert.Equal(HttpStatusCode.OK, conventionsResponse.StatusCode);
        var conventions = await conventionsResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(conventions);
        Assert.Contains(conventions!, c => c.GetProperty("id").GetGuid() == conventionId);

        var personsBeforeResponse = await client.GetAsync($"/system/tenants/{tenantId}/conventions/{conventionId}/persons");
        Assert.Equal(HttpStatusCode.OK, personsBeforeResponse.StatusCode);
        var personsBefore = await personsBeforeResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(personsBefore);
        Assert.Contains(personsBefore!, p => p.GetProperty("id").GetGuid() == personId && !p.GetProperty("isAdmin").GetBoolean());

        var addAdminResponse = await client.PostAsJsonAsync(
            $"/system/tenants/{tenantId}/conventions/{conventionId}/administrators",
            new { personId });
        Assert.Equal(HttpStatusCode.NoContent, addAdminResponse.StatusCode);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();

            var adminTenantId = await db.Set<ConventionAdministrator>()
                .Where(a => EF.Property<ConventionId>(a, "ConventionId") == new ConventionId(conventionId)
                            && a.PersonId == new PersonId(personId))
                .Select(a => EF.Property<Guid>(a, "TenantId"))
                .SingleAsync();
            Assert.Equal(tenantId, adminTenantId);

            var personTenantId = await db.Persons
                .Where(p => p.Id == new PersonId(personId))
                .Select(p => EF.Property<Guid>(p, "TenantId"))
                .SingleAsync();
            Assert.Equal(tenantId, personTenantId);
        }

        var personsAfterAddResponse = await client.GetAsync($"/system/tenants/{tenantId}/conventions/{conventionId}/persons");
        Assert.Equal(HttpStatusCode.OK, personsAfterAddResponse.StatusCode);
        var personsAfterAdd = await personsAfterAddResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(personsAfterAdd);
        Assert.Contains(personsAfterAdd!, p => p.GetProperty("id").GetGuid() == personId && p.GetProperty("isAdmin").GetBoolean());

        var removeAdminResponse = await client.DeleteAsync(
            $"/system/tenants/{tenantId}/conventions/{conventionId}/administrators/{personId}");
        Assert.Equal(HttpStatusCode.NoContent, removeAdminResponse.StatusCode);

        var personsAfterRemoveResponse = await client.GetAsync($"/system/tenants/{tenantId}/conventions/{conventionId}/persons");
        Assert.Equal(HttpStatusCode.OK, personsAfterRemoveResponse.StatusCode);
        var personsAfterRemove = await personsAfterRemoveResponse.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(personsAfterRemove);
        Assert.Contains(personsAfterRemove!, p => p.GetProperty("id").GetGuid() == personId && !p.GetProperty("isAdmin").GetBoolean());
    }

    [Fact]
    public async Task ProvisionTenantConvention_IsIdempotent_WhenTenantIsAlreadyProvisioned()
    {
        var token = CreateToken(new Claim("is_system_admin", "true"));
        var client = CreateAuthorizedClient(token);

        var subdomain = $"idem-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/system/tenants", new
        {
            subdomain,
            displayName = "Idempotent Tenant"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var tenantId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var restoreResponse = await client.PutAsync($"/system/tenants/{tenantId}/restore", content: null);
        Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);

        var adminEmail = $"idem-admin-{Guid.NewGuid():N}@test.se";

        var firstProvision = await client.PostAsJsonAsync($"/system/tenants/{tenantId}/provision", new
        {
            adminName = "Bootstrap Admin",
            adminEmail,
            adminPassword = "Admin123!"
        });
        Assert.Equal(HttpStatusCode.Created, firstProvision.StatusCode);
        var firstBody = await firstProvision.Content.ReadFromJsonAsync<JsonElement>();
        var conventionId = firstBody.GetProperty("conventionId").GetGuid();
        var adminUserId = firstBody.GetProperty("adminUserId").GetString();
        Assert.False(firstBody.GetProperty("alreadyProvisioned").GetBoolean());

        var secondProvision = await client.PostAsJsonAsync($"/system/tenants/{tenantId}/provision", new
        {
            adminName = "Bootstrap Admin",
            adminEmail,
            adminPassword = "Admin123!"
        });
        Assert.Equal(HttpStatusCode.OK, secondProvision.StatusCode);
        var secondBody = await secondProvision.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(conventionId, secondBody.GetProperty("conventionId").GetGuid());
        Assert.Equal(adminUserId, secondBody.GetProperty("adminUserId").GetString());
        Assert.True(secondBody.GetProperty("alreadyProvisioned").GetBoolean());

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();

        var conventionCount = await db.Conventions
            .CountAsync(c => EF.Property<Guid>(c, "TenantId") == tenantId);
        Assert.Equal(1, conventionCount);

        var userCount = await identityDb.Users
            .CountAsync(u => u.TenantId == tenantId && u.Email == adminEmail);
        Assert.Equal(1, userCount);
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
            Issuer = ConventionSystemFactory.TestJwtIssuer,
            Audience = ConventionSystemFactory.TestJwtAudience,
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

file sealed class CapturingEmailService : IEmailService
{
    public List<ProvisionedWelcomeEmail> ProvisionedWelcomeEmails { get; } = [];

    public Task SendVisitorRegistrationConfirmedAsync(string toEmail, string toName, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendStaffApplicationReceivedAsync(string toEmail, string toName, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendStaffApplicationAcceptedAsync(string toEmail, string toName, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendStaffApplicationRejectedAsync(string toEmail, string toName, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendEventApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendEventRejectedAsync(string toEmail, string toName, string eventTitle, string comment, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendPasswordResetAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendResendConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendPasswordChangedAsync(string toEmail, string toName, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendTenantSignupWelcomeAsync(
        string toEmail,
        string toName,
        string organizationName,
        string subdomain,
        string temporaryPassword,
        string confirmLink,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendTenantProvisionedWelcomeAsync(
        string toEmail,
        string toName,
        string organizationName,
        string subdomain,
        string temporaryPassword,
        string loginLink,
        CancellationToken ct = default)
    {
        ProvisionedWelcomeEmails.Add(new ProvisionedWelcomeEmail(
            toEmail,
            toName,
            organizationName,
            subdomain,
            temporaryPassword,
            loginLink));
        return Task.CompletedTask;
    }

    public Task SendCoOrganiserApplicationReceivedAsync(string toEmail, string eventTitle, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendCoOrganiserApplicationApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendCoOrganiserApplicationRejectedAsync(string toEmail, string toName, string eventTitle, string? comment, CancellationToken ct = default) => Task.CompletedTask;
}

file sealed record ProvisionedWelcomeEmail(
    string ToEmail,
    string ToName,
    string OrganizationName,
    string Subdomain,
    string TemporaryPassword,
    string LoginLink);
