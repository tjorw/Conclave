using ConventionSystem.Application.Abstractions;
using ConventionSystem.Application.Common;
using MediatR;

namespace ConventionSystem.Application.Behaviours;

internal sealed class TransactionBehaviour<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
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
