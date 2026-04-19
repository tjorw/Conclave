namespace ConventionSystem.Application.Abstractions;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
    Task ExecuteAsync(Func<Task> operation, CancellationToken ct = default);
}
