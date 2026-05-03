using System.Net;
using ConventionSystem.Integration.Tests.Infrastructure;

namespace ConventionSystem.Integration.Tests.Uploads;

public sealed class UploadEndpointsTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UploadImage_WithValidFile_ReturnsPublicUrl()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        using var content = CreateMultipart([1, 2, 3, 4], "image/png", "poster.png");

        var response = await client.PostAsync("/api/uploads", content);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var url = body.GetProperty("url").GetString();

        Assert.NotNull(url);
        Assert.StartsWith("/uploads/00000000000000000000000000000000/", url);
        Assert.EndsWith(".png", url);

        var fileResponse = await client.GetAsync(url);
        fileResponse.EnsureSuccessStatusCode();
        Assert.Equal([1, 2, 3, 4], await fileResponse.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UploadImage_WithInvalidContentType_ReturnsBadRequest()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        using var content = CreateMultipart([1, 2, 3], "text/plain", "not-image.txt");

        var response = await client.PostAsync("/api/uploads", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_file_type", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UploadImage_WithTooLargeFile_ReturnsBadRequest()
    {
        var token = await LoginAsync(AdminEmail, AdminPassword);
        var client = CreateClient(token);
        using var content = CreateMultipart(new byte[(5 * 1024 * 1024) + 1], "image/jpeg", "huge.jpg");

        var response = await client.PostAsync("/api/uploads", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("file_too_large", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UploadImage_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = CreateClient();
        using var content = CreateMultipart([1, 2, 3], "image/png", "poster.png");

        var response = await client.PostAsync("/api/uploads", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static MultipartFormDataContent CreateMultipart(byte[] bytes, string contentType, string filename)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", filename);
        return content;
    }
}
