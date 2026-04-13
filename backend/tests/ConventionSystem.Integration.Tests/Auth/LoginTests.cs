using System.Net;
using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.Auth;

public sealed class LoginTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var response = await Factory.CreateClient()
            .PostAsJsonAsync("/auth/login", new { email = AdminEmail, password = AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenContainsPersonIdClaim()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        var claims = ParseClaims(token);
        Assert.Contains(claims, c => c.Type == "person_id" && Guid.TryParse(c.Value, out _));
    }

    [Fact]
    public async Task Login_AsAdmin_TokenContainsIsAdminClaim()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);

        var claims = ParseClaims(token);
        Assert.Contains(claims, c => c.Type == "is_admin" && c.Value == "true");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await Factory.CreateClient()
            .PostAsJsonAsync("/auth/login", new { email = AdminEmail, password = "FelLösenord9" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await Factory.CreateClient()
            .PostAsJsonAsync("/auth/login", new { email = "okänd@test.com", password = "Test1234" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
