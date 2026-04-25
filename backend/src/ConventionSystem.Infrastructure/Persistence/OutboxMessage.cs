namespace ConventionSystem.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ProcessAfter { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string payload) => new()
    {
        Id = Guid.CreateVersion7(),
        Type = type,
        Payload = payload,
        CreatedAt = DateTimeOffset.UtcNow,
        ProcessAfter = DateTimeOffset.UtcNow
    };

    public void MarkProcessed() => ProcessedAt = DateTimeOffset.UtcNow;

    public void MarkFailed(string error, DateTimeOffset retryAfter)
    {
        RetryCount++;
        Error = error;
        ProcessAfter = retryAfter;
    }
}
