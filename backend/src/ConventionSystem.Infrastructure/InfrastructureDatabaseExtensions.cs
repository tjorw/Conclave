using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Infrastructure;

public static class InfrastructureDatabaseExtensions
{
    public static async Task MigrateInfrastructureDatabasesAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;

        await serviceProvider.GetRequiredService<ConventionDbContext>().Database.MigrateAsync();
        await serviceProvider.GetRequiredService<ApplicationIdentityDbContext>().Database.MigrateAsync();
    }
}
