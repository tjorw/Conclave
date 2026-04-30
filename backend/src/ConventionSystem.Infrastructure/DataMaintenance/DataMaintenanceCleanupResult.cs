namespace ConventionSystem.Infrastructure.DataMaintenance;

public sealed record DataMaintenanceCleanupResult(
    int ProcessedOutboxMessagesDeleted,
    int FailedOutboxMessagesDeleted,
    int DomainEventLogEntriesDeleted)
{
    public static DataMaintenanceCleanupResult Empty { get; } = new(0, 0, 0);
}
