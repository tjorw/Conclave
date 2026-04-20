namespace ConventionSystem.Application.Common;

public interface ISender
{
    Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken ct = default);
}
