using System.Reflection;
using ConventionSystem.Application.Behaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ConventionSystem.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehaviour<,>));
        });

        return services;
    }
}
