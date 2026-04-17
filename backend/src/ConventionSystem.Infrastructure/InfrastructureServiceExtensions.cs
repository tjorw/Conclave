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
using Microsoft.Extensions.Options;

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

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        services.AddScoped<LoggingEmailService>();
        services.AddScoped<SmtpEmailService>();
        services.AddScoped<SendGridEmailService>();

        services.AddScoped<IEmailService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;

            return options.Provider.ToLowerInvariant() switch
            {
                "smtp" => provider.GetRequiredService<SmtpEmailService>(),
                "sendgrid" => provider.GetRequiredService<SendGridEmailService>(),
                "logging" => provider.GetRequiredService<LoggingEmailService>(),
                _ => throw new InvalidOperationException(
                    $"Ogiltig e-postprovider '{options.Provider}'. Tillatna varden ar 'Logging', 'Smtp' och 'SendGrid'.")
            };
        });

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
