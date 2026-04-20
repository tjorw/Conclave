using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Application.Common;

public sealed class Mediator(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> _cache = new();

    public Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken ct = default)
    {
        var method = _cache.GetOrAdd(request.GetType(), t =>
            typeof(Mediator)
                .GetMethod(nameof(Dispatch), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(t, typeof(TResult)));

        return (Task<TResult>)method.Invoke(this, [request, ct])!;
    }

    private Task<TResult> Dispatch<TRequest, TResult>(TRequest request, CancellationToken ct)
        where TRequest : IRequest<TResult>
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResult>>();
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResult>>()
            .Reverse()
            .ToList();

        RequestHandlerDelegate<TResult> pipeline = () => handler.Handle(request, ct);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            var b = behavior;
            pipeline = () => b.Handle(request, next, ct);
        }

        return pipeline();
    }
}
