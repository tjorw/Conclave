using System.Net;
using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.Feed;

public sealed class FeedEndpointTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetEditionFeed_UnknownId_Returns404()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync($"/feed/editions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEventFeed_UnknownId_Returns404()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync($"/feed/events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEditionFeed_NoAuthorizationHeader_Returns200OrNotFound()
    {
        // Verifiera att feed-endpoints inte kräver autentisering – ingen 401 ska returneras.
        // (Endpoint finns men eventuell data saknas – 404 är OK, men aldrig 401.)
        var (conventionId, _, _, _) = await ProvisionAsync();

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Convention-Id", conventionId.ToString());
        var response = await client.GetAsync($"/feed/editions/{Guid.NewGuid()}");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
