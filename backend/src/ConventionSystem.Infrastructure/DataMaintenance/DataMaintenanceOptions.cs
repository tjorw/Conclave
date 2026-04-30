namespace ConventionSystem.Infrastructure.DataMaintenance;

public sealed class DataMaintenanceOptions
{
    public const string SectionName = "DataMaintenance";

    public bool Enabled { get; set; }
    public int OutboxProcessedRetentionDays { get; set; } = 30;
    public int OutboxFailedRetentionDays { get; set; } = 90;
    public int DomainEventLogRetentionDays { get; set; } = 180;
    public int BatchSize { get; set; } = 500;
}
