using ConventionSystem.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence;

internal sealed class UnitOfWork(ConventionDbContext dbContext) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken ct = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        }, ct);
    }
}
