using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Registration.Services;
using ConventionSystem.Infrastructure.Dispatching;
using ConventionSystem.Infrastructure.Email;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using ConventionSystem.Infrastructure.Persistence.Repositories;
using ConventionSystem.Infrastructure.Registration;
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
        services.AddScoped<IEventRepository, EventRepository>();

        services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IVisitorRegistrationRepository, VisitorRegistrationRepository>();
        services.AddScoped<IStaffApplicationRepository, StaffApplicationRepository>();
        services.AddScoped<ISessionRegistrationRepository, SessionRegistrationRepository>();
        services.AddScoped<ISessionWatchRepository, SessionWatchRepository>();
        services.AddScoped<IMyScheduleRepository, MyScheduleRepository>();
        services.AddScoped<IRegistrationRuleService, StubRegistrationRuleService>();

        services.AddScoped<IEmailService, LoggingEmailService>();

        services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
        services.AddScoped<EventDispatchInterceptor>();

        services.AddDbContext<ConventionDbContext>((provider, options) =>
        {
            var interceptor = provider.GetRequiredService<EventDispatchInterceptor>();
            options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(interceptor);
        });

        services.AddDbContext<ApplicationIdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>();

        return services;
    }
}
