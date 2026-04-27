using System.Text.Json;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ConventionSystem.Infrastructure.Email;

internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private static readonly ResiliencePipeline SmtpPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(2),
            UseJitter = true
        })
        .Build();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatchAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConventionDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IDirectEmailSender>();

        var now = DateTimeOffset.UtcNow;
        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.ProcessAfter <= now)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            if (ct.IsCancellationRequested)
                break;

            await ProcessMessageAsync(db, sender, message, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ProcessMessageAsync(
        ConventionDbContext db,
        IDirectEmailSender sender,
        OutboxMessage message,
        CancellationToken ct)
    {
        EmailPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<EmailPayload>(message.Payload)
                ?? throw new InvalidOperationException("Null payload i outbox-meddelande.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kunde inte deserialisera outbox-meddelande {Id} – markeras som misslyckat.", message.Id);
            message.MarkFailed(ex.Message, DateTimeOffset.MaxValue);
            return;
        }

        try
        {
            await SmtpPipeline.ExecuteAsync(async token => await sender.SendAsync(payload, token), ct);
            message.MarkProcessed();
            logger.LogInformation("Outbox-meddelande {Id} skickat till {To}.", message.Id, payload.To);
        }
        catch (Exception ex)
        {
            var retryAfter = ComputeRetryAfter(message.RetryCount);
            logger.LogWarning(ex, "Misslyckades skicka outbox-meddelande {Id} (försök {Attempt}). Nästa försök: {RetryAfter}.",
                message.Id, message.RetryCount + 1, retryAfter);
            message.MarkFailed(ex.Message, retryAfter);
        }
    }

    private static DateTimeOffset ComputeRetryAfter(int currentRetryCount) => currentRetryCount switch
    {
        0 => DateTimeOffset.UtcNow.AddMinutes(2),
        1 => DateTimeOffset.UtcNow.AddMinutes(8),
        2 => DateTimeOffset.UtcNow.AddMinutes(30),
        _ => DateTimeOffset.UtcNow.AddHours(2)
    };
}
