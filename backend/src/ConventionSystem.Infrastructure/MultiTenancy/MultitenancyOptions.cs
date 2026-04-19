namespace ConventionSystem.Infrastructure.MultiTenancy;

public sealed class MultitenancyOptions
{
    public const string SectionName = "Multitenancy";

    public bool Enabled { get; set; }
    public string DefaultSubdomain { get; set; } = "default";
}