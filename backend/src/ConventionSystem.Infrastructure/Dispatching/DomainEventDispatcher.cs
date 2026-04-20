using System.Collections.Concurrent;
using System.Reflection;
using ConventionSystem.Application.Common;
using ConventionSystem.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Infrastructure.Dispatching;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> _cache = new();

    public async Task DispatchAndClearEvents(
        IEnumerable<AggregateRoot> aggregatesWithEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var aggregate in aggregatesWithEvents)
        {
            var events = aggregate.DomainEvents.ToArray();
            aggregate.ClearDomainEvents();

            foreach (var domainEvent in events)
                await DispatchEvent(domainEvent, cancellationToken);
        }
    }

    private Task DispatchEvent(IDomainEvent domainEvent, CancellationToken ct)
    {
        var method = _cache.GetOrAdd(domainEvent.GetType(), t =>
            typeof(DomainEventDispatcher)
                .GetMethod(nameof(DispatchEventTyped), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(t));

        return (Task)method.Invoke(this, [domainEvent, ct])!;
    }

    private async Task DispatchEventTyped<TEvent>(TEvent domainEvent, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TEvent>>())
            await handler.Handle(domainEvent, ct);
    }
}
