using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Infrastructure.DataMaintenance;

public sealed class DataMaintenanceCleanupService(
    ConventionDbContext db,
    IOptions<DataMaintenanceOptions> optionsAccessor,
    ILogger<DataMaintenanceCleanupService> logger)
{
    public async Task<DataMaintenanceCleanupResult> RunOnceAsync(CancellationToken ct = default)
    {
        var options = optionsAccessor.Value;
        if (!options.Enabled)
        {
            logger.LogDebug("Dataunderhall ar avstangt.");
            return DataMaintenanceCleanupResult.Empty;
        }

        var now = DateTimeOffset.UtcNow;
        var batchSize = Math.Max(1, options.BatchSize);

        var processedOutboxDeleted = await RunRuleAsync(
            "processed outbox cleanup",
            () => DeleteProcessedOutboxMessagesAsync(now, options.OutboxProcessedRetentionDays, batchSize, ct));

        var failedOutboxDeleted = await RunRuleAsync(
            "failed outbox cleanup",
            () => DeleteFailedOutboxMessagesAsync(now, options.OutboxFailedRetentionDays, batchSize, ct));

        var domainEventLogDeleted = await RunRuleAsync(
            "domain event log cleanup",
            () => DeleteDomainEventLogEntriesAsync(now, options.DomainEventLogRetentionDays, batchSize, ct));

        logger.LogInformation(
            "Dataunderhall klart. Borttaget: {ProcessedOutboxMessages} skickade outbox-meddelanden, {FailedOutboxMessages} parkerade outbox-meddelanden, {DomainEventLogEntries} domain event-loggrader.",
            processedOutboxDeleted,
            failedOutboxDeleted,
            domainEventLogDeleted);

        return new DataMaintenanceCleanupResult(
            processedOutboxDeleted,
            failedOutboxDeleted,
            domainEventLogDeleted);
    }

    private async Task<int> RunRuleAsync(string ruleName, Func<Task<int>> rule)
    {
        try
        {
            return await rule();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dataunderhall-regeln {RuleName} misslyckades.", ruleName);
            return 0;
        }
    }

    private async Task<int> DeleteProcessedOutboxMessagesAsync(
        DateTimeOffset now,
        int retentionDays,
        int batchSize,
        CancellationToken ct)
    {
        var cutoff = now.AddDays(-Math.Max(0, retentionDays));

        return await DeleteInBatchesAsync<OutboxMessage>(
            query => query
                .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoff)
                .OrderBy(m => m.ProcessedAt)
                .ThenBy(m => m.CreatedAt),
            batchSize,
            ct);
    }

    private async Task<int> DeleteFailedOutboxMessagesAsync(
        DateTimeOffset now,
        int retentionDays,
        int batchSize,
        CancellationToken ct)
    {
        var cutoff = now.AddDays(-Math.Max(0, retentionDays));

        return await DeleteInBatchesAsync<OutboxMessage>(
            query => query
                .Where(m => m.ProcessedAt == null
                    && m.ProcessAfter == DateTimeOffset.MaxValue
                    && m.CreatedAt < cutoff)
                .OrderBy(m => m.CreatedAt),
            batchSize,
            ct);
    }

    private async Task<int> DeleteDomainEventLogEntriesAsync(
        DateTimeOffset now,
        int retentionDays,
        int batchSize,
        CancellationToken ct)
    {
        var cutoff = now.AddDays(-Math.Max(0, retentionDays));

        return await DeleteInBatchesAsync<DomainEventLogEntry>(
            query => query
                .Where(e => e.OccurredAt < cutoff)
                .OrderBy(e => e.OccurredAt),
            batchSize,
            ct);
    }

    private async Task<int> DeleteInBatchesAsync<TEntity>(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> filter,
        int batchSize,
        CancellationToken ct)
        where TEntity : class
    {
        var totalDeleted = 0;

        while (!ct.IsCancellationRequested)
        {
            var ids = await filter(db.Set<TEntity>())
                .Select(entity => EF.Property<Guid>(entity, "Id"))
                .Take(batchSize)
                .ToListAsync(ct);

            if (ids.Count == 0)
                return totalDeleted;

            var deleted = await db.Set<TEntity>()
                .Where(entity => ids.Contains(EF.Property<Guid>(entity, "Id")))
                .ExecuteDeleteAsync(ct);

            totalDeleted += deleted;

            if (ids.Count < batchSize)
                return totalDeleted;
        }

        return totalDeleted;
    }
}
