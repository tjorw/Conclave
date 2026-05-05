using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ConventionSystem.Application.Common;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Infrastructure.FileStorage;

public sealed class BlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private readonly string? _publicBaseUrl;

    public BlobFileStorage(IOptions<FileStorageOptions> options)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.BlobConnectionString))
            throw new InvalidOperationException(
                "FileStorage:BlobConnectionString måste konfigureras när Provider är 'Blob'.");

        var serviceClient = new BlobServiceClient(opts.BlobConnectionString);
        _container = serviceClient.GetBlobContainerClient(opts.BlobContainerName);
        _publicBaseUrl = string.IsNullOrWhiteSpace(opts.BlobPublicBaseUrl) ? null : opts.BlobPublicBaseUrl.TrimEnd('/');
    }

    public async Task<string> UploadAsync(
        string tenantId,
        string originalFilename,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var extension = ExtensionFor(contentType);
        var blobName = $"{tenantId}/{Guid.CreateVersion7():N}{extension}";
        var blobClient = _container.GetBlobClient(blobName);

        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        return _publicBaseUrl is not null
            ? $"{_publicBaseUrl}/{blobName}"
            : blobClient.Uri.ToString();
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
}
