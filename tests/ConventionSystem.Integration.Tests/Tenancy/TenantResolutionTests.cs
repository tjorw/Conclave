using System.Net;
using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.Tenancy;

public sealed class TenantResolutionTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Request_WithoutConventionHeader_ProtectedEndpointReturns401()
    {
        // Skyddad endpoint utan header → autentiseringen misslyckas (inget tenant = ingen person)
        var client = Factory.CreateClient();
        var response = await client.PutAsJsonAsync("/me/profile",
            new { name = "Test", email = "test@test.com", phone = (string?)null });

        // 401 från JWT-middleware (ingen token) eller 400 från tenantmiddleware – båda är OK
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest,
            $"Förväntade 401 eller 400, fick {response.StatusCode}");
    }

    [Fact]
    public async Task Request_WithUnknownConventionId_LoginReturns400()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Convention-Id", Guid.NewGuid().ToString());
        var response = await client.PostAsJsonAsync("/auth/login",
            new { email = "x@x.com", password = "Test1234" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithValidConventionId_LoginSucceeds()
    {
        var (conventionId, email, password, _) = await ProvisionAsync();

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Convention-Id", conventionId.ToString());
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
