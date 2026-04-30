using ConventionSystem.Infrastructure.DataMaintenance;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Integration.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Integration.Tests.DataMaintenance;

public sealed class DataMaintenanceCleanupServiceTests(ConventionSystemFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task RunOnceAsync_DeletesExpiredInfrastructureRows()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var cleanup = CreateCleanupService(db, enabled: true);

        var oldProcessedOutboxId = Guid.CreateVersion7();
        var oldFailedOutboxId = Guid.CreateVersion7();
        var oldDomainEventId = Guid.CreateVersion7();

        await InsertOutboxMessageAsync(
            db,
            oldProcessedOutboxId,
            createdAt: DateTimeOffset.UtcNow.AddDays(-45),
            processAfter: DateTimeOffset.UtcNow.AddDays(-45),
            processedAt: DateTimeOffset.UtcNow.AddDays(-31));

        await InsertOutboxMessageAsync(
            db,
            oldFailedOutboxId,
            createdAt: DateTimeOffset.UtcNow.AddDays(-100),
            processAfter: DateTimeOffset.MaxValue,
            processedAt: null);

        await InsertDomainEventLogEntryAsync(
            db,
            oldDomainEventId,
            occurredAt: DateTimeOffset.UtcNow.AddDays(-181));

        var result = await cleanup.RunOnceAsync();

        Assert.True(result.ProcessedOutboxMessagesDeleted >= 1);
        Assert.True(result.FailedOutboxMessagesDeleted >= 1);
        Assert.True(result.DomainEventLogEntriesDeleted >= 1);
        Assert.False(await db.OutboxMessages.AnyAsync(m => m.Id == oldProcessedOutboxId));
        Assert.False(await db.OutboxMessages.AnyAsync(m => m.Id == oldFailedOutboxId));
        Assert.False(await db.DomainEventLog.AnyAsync(e => e.Id == oldDomainEventId));
    }

    [Fact]
    public async Task RunOnceAsync_KeepsActiveOutboxMessages()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var cleanup = CreateCleanupService(db, enabled: true);

        var activeOutboxId = Guid.CreateVersion7();

        await InsertOutboxMessageAsync(
            db,
            activeOutboxId,
            createdAt: DateTimeOffset.UtcNow.AddDays(-120),
            processAfter: DateTimeOffset.UtcNow.AddMinutes(-1),
            processedAt: null);

        await cleanup.RunOnceAsync();

        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Id == activeOutboxId));
    }

    [Fact]
    public async Task RunOnceAsync_WhenDisabled_DoesNotDeleteRows()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var cleanup = CreateCleanupService(db, enabled: false);

        var oldProcessedOutboxId = Guid.CreateVersion7();

        await InsertOutboxMessageAsync(
            db,
            oldProcessedOutboxId,
            createdAt: DateTimeOffset.UtcNow.AddDays(-45),
            processAfter: DateTimeOffset.UtcNow.AddDays(-45),
            processedAt: DateTimeOffset.UtcNow.AddDays(-31));

        var result = await cleanup.RunOnceAsync();

        Assert.Equal(DataMaintenanceCleanupResult.Empty, result);
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Id == oldProcessedOutboxId));
    }

    private static DataMaintenanceCleanupService CreateCleanupService(ConventionDbContext db, bool enabled)
    {
        var options = Options.Create(new DataMaintenanceOptions
        {
            Enabled = enabled,
            OutboxProcessedRetentionDays = 30,
            OutboxFailedRetentionDays = 90,
            DomainEventLogRetentionDays = 180,
            BatchSize = 2
        });

        return new DataMaintenanceCleanupService(
            db,
            options,
            NullLogger<DataMaintenanceCleanupService>.Instance);
    }

    private static async Task InsertOutboxMessageAsync(
        ConventionDbContext db,
        Guid id,
        DateTimeOffset createdAt,
        DateTimeOffset processAfter,
        DateTimeOffset? processedAt)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO outbox_messages
                (Id, Type, Payload, created_at, process_after, processed_at, retry_count, error)
            VALUES
                (@id, @type, @payload, @createdAt, @processAfter, @processedAt, @retryCount, @error)
            """,
            CreateParameter("@id", id),
            CreateParameter("@type", "EmailMessage"),
            CreateParameter("@payload", "{}"),
            CreateParameter("@createdAt", createdAt),
            CreateParameter("@processAfter", processAfter),
            CreateParameter("@processedAt", (object?)processedAt ?? DBNull.Value),
            CreateParameter("@retryCount", 0),
            CreateParameter("@error", DBNull.Value));
    }

    private static async Task InsertDomainEventLogEntryAsync(
        ConventionDbContext db,
        Guid id,
        DateTimeOffset occurredAt)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO domain_event_log
                (Id, EventType, Payload, occurred_at)
            VALUES
                (@id, @eventType, @payload, @occurredAt)
            """,
            CreateParameter("@id", id),
            CreateParameter("@eventType", "TestEvent"),
            CreateParameter("@payload", "{}"),
            CreateParameter("@occurredAt", occurredAt));
    }

    private static SqlParameter CreateParameter(string name, object value) => new()
    {
        ParameterName = name,
        Value = value
    };
}
