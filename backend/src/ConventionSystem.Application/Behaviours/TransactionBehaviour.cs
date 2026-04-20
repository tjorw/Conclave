using ConventionSystem.Application.Abstractions;
using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Behaviours;

internal sealed class TransactionBehaviour<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is IQuery<TResponse>)
            return await next();

        return await unitOfWork.ExecuteAsync(() => next(), ct);
    }
}
