using ConventionSystem.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Infrastructure.FileStorage;

public sealed class LocalDiskFileStorage(
    IWebHostEnvironment environment,
    IOptions<FileStorageOptions> options) : IFileStorage
{
    public async Task<string> UploadAsync(
        string tenantId,
        string originalFilename,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        var extension = ExtensionFor(contentType);
        var normalizedTenantId = NormalizePathSegment(tenantId);
        var filename = $"{Guid.CreateVersion7():N}{extension}";
        var rootPath = string.IsNullOrWhiteSpace(options.Value.LocalRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot", "uploads")
            : options.Value.LocalRootPath;
        var uploadRoot = Path.Combine(rootPath, normalizedTenantId);

        Directory.CreateDirectory(uploadRoot);

        var fullPath = Path.Combine(uploadRoot, filename);
        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, ct);

        return $"/uploads/{normalizedTenantId}/{filename}";
    }

    private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/svg+xml" => ".svg",
        "image/webp" => ".webp",
        _ => throw new InvalidOperationException($"Unsupported content type '{contentType}'.")
    };

    private static string NormalizePathSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tenant id is required.", nameof(value));

        var normalized = value.Trim();
        return normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            ? throw new ArgumentException("Tenant id contains invalid path characters.", nameof(value))
            : normalized;
    }
}
