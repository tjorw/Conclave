using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ConventionSystem.Integration.Tests.Infrastructure;

[Collection(ConventionTestCollection.Name)]
public abstract class IntegrationTestBase(ConventionSystemFactory factory)
{
    protected ConventionSystemFactory Factory { get; } = factory;

    private static int _dbCounter;

    /// <summary>
    /// Provisionerar en ny konvention med egen databas.
    /// Kör ConventionDb-migrationer, anropar POST /system/conventions
    /// och returnerar konventions-ID och admin-credentials.
    /// </summary>
    protected async Task<ProvisionResult> ProvisionAsync()
    {
        var suffix = Interlocked.Increment(ref _dbCounter);
        var dbName = $"ConventionTest{suffix}";
        var connectionString = Factory.GetConnectionString(dbName);

        await Factory.MigrateConventionDbAsync(connectionString);

        var slug = $"test{suffix}";
        var email = $"admin{suffix}@test.com";
        const string password = "Test1234";

        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/system/conventions", new
        {
            Name = "Test Convention",
            Slug = slug,
            RegistrantName = "Admin",
            RegistrantEmail = email,
            RegistrantPassword = password,
            ConnectionString = connectionString
        });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var conventionId = body.GetProperty("conventionId").GetGuid();

        return new ProvisionResult(conventionId, email, password, connectionString);
    }

    /// <summary>
    /// Loggar in och returnerar JWT.
    /// </summary>
    protected async Task<string> LoginAsync(Guid conventionId, string email, string password)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Convention-Id", conventionId.ToString());

        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    /// <summary>
    /// Skapar en HttpClient med X-Convention-Id-header förifylld och valfri Bearer-token.
    /// </summary>
    protected HttpClient CreateClient(Guid conventionId, string? token = null)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Convention-Id", conventionId.ToString());
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

public record ProvisionResult(Guid ConventionId, string Email, string Password, string ConnectionString);
