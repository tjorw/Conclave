namespace ConventionSystem.Application.Common;

public interface IRequestHandler<TRequest, TResult> where TRequest : IRequest<TResult>
{
    Task<TResult> Handle(TRequest request, CancellationToken ct);
}
