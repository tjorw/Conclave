using ConventionSystem.Application.Common;

namespace ConventionSystem.Infrastructure.FileStorage;

public sealed class BlobFileStorage : IFileStorage
{
    public Task<string> UploadAsync(
        string tenantId,
        string originalFilename,
        Stream content,
        string contentType,
        CancellationToken ct = default)
        => throw new NotSupportedException("Blob file storage is not implemented yet.");
}
