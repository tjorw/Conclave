using System.Net;
using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.Auth;

public sealed class LoginTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var (conventionId, email, password, _) = await ProvisionAsync();

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Convention-Id", conventionId.ToString());
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenContainsPersonIdClaim()
    {
        var (conventionId, email, password, _) = await ProvisionAsync();
        var token = await LoginAsync(conventionId, email, password);

        var claims = ParseClaims(token);
        Assert.Contains(claims, c => c.Type == "person_id" && Guid.TryParse(c.Value, out _));
    }

    [Fact]
    public async Task Login_AsAdmin_TokenContainsIsAdminClaim()
    {
        var (conventionId, email, password, _) = await ProvisionAsync();
        var token = await LoginAsync(conventionId, email, password);

        var claims = ParseClaims(token);
        Assert.Contains(claims, c => c.Type == "is_admin" && c.Value == "true");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var (conventionId, email, _, _) = await ProvisionAsync();

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Convention-Id", conventionId.ToString());
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password = "FelLösenord9" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingConventionHeader_Returns400()
    {
        await ProvisionAsync();

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login",
            new { email = "admin@test.com", password = "Test1234" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownConventionId_Returns400()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Convention-Id", Guid.NewGuid().ToString());
        var response = await client.PostAsJsonAsync("/auth/login",
            new { email = "admin@test.com", password = "Test1234" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
