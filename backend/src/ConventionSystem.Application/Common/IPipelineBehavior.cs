namespace ConventionSystem.Application.Common;

public delegate Task<TResult> RequestHandlerDelegate<TResult>();

public interface IPipelineBehavior<TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> Handle(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct);
}
