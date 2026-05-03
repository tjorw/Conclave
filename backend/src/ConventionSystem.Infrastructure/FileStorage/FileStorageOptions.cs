namespace ConventionSystem.Infrastructure.FileStorage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string Provider { get; init; } = "Local";
    public int MaxSizeMb { get; init; } = 5;
    public string? LocalRootPath { get; init; }
}
