using System.Reflection;
using ConventionSystem.Application.Behaviours;
using ConventionSystem.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var serviceType in type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            {
                services.AddTransient(serviceType, type);
            }

            foreach (var serviceType in type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            {
                services.AddTransient(serviceType, type);
            }
        }

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehaviour<,>));
        services.AddScoped<ISender, Mediator>();

        return services;
    }
}
