using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ConventionSystem.Integration.Tests.Infrastructure;

[Collection(ConventionTestCollection.Name)]
public abstract class IntegrationTestBase(ConventionSystemFactory factory)
{
    // Inloggningsuppgifter för den seedade admin-användaren (se ConventionSystemFactory.SetupConventionAsync)
    protected string AdminEmail => ConventionSystemFactory.AdminEmail;
    protected string AdminPassword => ConventionSystemFactory.AdminPassword;

    protected ConventionSystemFactory Factory { get; } = factory;

    /// <summary>
    /// Loggar in och returnerar JWT.
    /// </summary>
    protected async Task<string> LoginAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Skapar en HttpClient med valfri Bearer-token.
    /// </summary>
    protected HttpClient CreateClient(string? token = null)
    {
        var client = Factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected static IEnumerable<Claim> ParseClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims;
    }
}
