using System.Net;
using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.Feed;

public sealed class FeedEndpointTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetEditionFeed_UnknownId_Returns404()
    {
        var conventionId = await Factory.GetConventionIdAsync();
        var client = Factory.CreateClient();

        var response = await client.GetAsync($"/feed/{conventionId}/editions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEventFeed_UnknownId_Returns404()
    {
        var conventionId = await Factory.GetConventionIdAsync();
        var client = Factory.CreateClient();

        var response = await client.GetAsync($"/feed/{conventionId}/events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEditionFeed_NoAuthorizationHeader_NeverReturns401()
    {
        // Feed-endpoints kräver inte autentisering
        var conventionId = await Factory.GetConventionIdAsync();
        var client = Factory.CreateClient();

        var response = await client.GetAsync($"/feed/{conventionId}/editions/{Guid.NewGuid()}");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
