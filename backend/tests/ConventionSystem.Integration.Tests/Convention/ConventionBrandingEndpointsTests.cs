using System.Net;
using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.Branding;

public sealed class ConventionBrandingEndpointsTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetBranding_WhenMissing_ReturnsNotFound()
    {
        var response = await CreateClient().GetAsync($"/conventions/{Guid.NewGuid()}/branding");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutBranding_ThenGetBranding_ReturnsBrandingWithCacheHeader()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var adminClient = CreateClient(token);

        var put = await adminClient.PutAsJsonAsync($"/conventions/{Factory.SeededConventionId}/branding", new
        {
            primaryColor = "#112233",
            accentColor = "#aabbcc",
            logoUrl = "/uploads/logo.svg",
            faviconUrl = "/uploads/favicon.png",
            fontFamily = "Inter",
            customCss = "--brand-primary: #112233;"
        });

        put.EnsureSuccessStatusCode();

        var response = await CreateClient().GetAsync($"/conventions/{Factory.SeededConventionId}/branding");

        response.EnsureSuccessStatusCode();
        Assert.Equal("max-age=300", response.Headers.CacheControl?.ToString());

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("#112233", body.GetProperty("primaryColor").GetString());
        Assert.Equal("#aabbcc", body.GetProperty("accentColor").GetString());
        Assert.Equal("/uploads/logo.svg", body.GetProperty("logoUrl").GetString());
        Assert.Equal("Inter", body.GetProperty("fontFamily").GetString());
    }

    [Fact]
    public async Task PutBranding_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await CreateClient().PutAsJsonAsync($"/conventions/{Factory.SeededConventionId}/branding", new
        {
            primaryColor = "#112233",
            accentColor = "#aabbcc",
            fontFamily = "Inter"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutBranding_WithInvalidHex_ReturnsUnprocessableEntity()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);

        var response = await client.PutAsJsonAsync($"/conventions/{Factory.SeededConventionId}/branding", new
        {
            primaryColor = "112233",
            accentColor = "#aabbcc",
            fontFamily = "Inter"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
