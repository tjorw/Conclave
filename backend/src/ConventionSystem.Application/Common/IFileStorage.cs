namespace ConventionSystem.Application.Common;

public interface IFileStorage
{
    Task<string> UploadAsync(
        string tenantId,
        string originalFilename,
        Stream content,
        string contentType,
        CancellationToken ct = default);
}
