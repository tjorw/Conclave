using System.Text.Json;
using ConventionSystem.Domain.Common;

namespace ConventionSystem.Infrastructure.Persistence;

public sealed class DomainEventLogEntry
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }

    private DomainEventLogEntry() { }

    public static DomainEventLogEntry From(IDomainEvent @event) => new()
    {
        Id = Guid.CreateVersion7(),
        EventType = @event.GetType().Name,
        Payload = JsonSerializer.Serialize(@event, @event.GetType()),
        OccurredAt = DateTimeOffset.UtcNow
    };
}
