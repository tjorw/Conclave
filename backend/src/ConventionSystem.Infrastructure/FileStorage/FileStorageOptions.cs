namespace ConventionSystem.Infrastructure.FileStorage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string Provider { get; init; } = "Local";
    public int MaxSizeMb { get; init; } = 5;
    public string? LocalRootPath { get; init; }

    // Blob provider
    public string? BlobConnectionString { get; init; }
    public string BlobContainerName { get; init; } = "uploads";
    // Optional CDN or custom domain prefix. If null, the Azure blob URL is used.
    public string? BlobPublicBaseUrl { get; init; }
}
