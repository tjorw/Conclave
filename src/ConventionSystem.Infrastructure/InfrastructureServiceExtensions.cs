using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Infrastructure.Dispatching;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IConventionRepository, ConventionRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IEditionRepository, EditionRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();

        services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
        services.AddScoped<EventDispatchInterceptor>();

        services.AddDbContext<ConventionDbContext>((provider, options) =>
        {
            var interceptor = provider.GetRequiredService<EventDispatchInterceptor>();
            options
                .UseSqlServer(configuration.GetConnectionString("ConventionDb"))
                .AddInterceptors(interceptor);
        });

        return services;
    }
}
