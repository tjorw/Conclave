using ConventionSystem.Application.Abstractions;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Export.Abstractions;
using ConventionSystem.Application.Reception.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Application.Tenancy.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Registration.Services;
using ConventionSystem.Infrastructure.DataMaintenance;
using ConventionSystem.Infrastructure.Dispatching;
using ConventionSystem.Infrastructure.Email;
using ConventionSystem.Infrastructure.FileStorage;
using Microsoft.Extensions.Hosting;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.MultiTenancy;
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
        services.AddScoped<IConventionBrandingRepository, ConventionBrandingRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IMailTemplateRepository, MailTemplateRepository>();
        services.AddScoped<IMailTemplateRenderer, MarkdigMailTemplateRenderer>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IEditionRepository, EditionRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEditionExportReadService, EditionExportReadService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ISystemTenantReadService, SystemTenantReadService>();

        services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPromotionCodeRepository, PromotionCodeRepository>();
        services.AddScoped<IVisitorRegistrationRepository, VisitorRegistrationRepository>();
        services.AddScoped<IStaffApplicationRepository, StaffApplicationRepository>();
        services.AddScoped<ISessionRegistrationRepository, SessionRegistrationRepository>();
        services.AddScoped<ISessionWatchRepository, SessionWatchRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamEventRegistrationRepository, TeamEventRegistrationRepository>();
        services.AddScoped<IMyScheduleRepository, MyScheduleRepository>();
        services.AddScoped<IReceptionScheduleRepository, ReceptionScheduleRepository>();
        services.AddScoped<IRegistrationRuleService, RegistrationRuleService>();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName));

        services.AddOptions<DataMaintenanceOptions>()
            .Bind(configuration.GetSection(DataMaintenanceOptions.SectionName));

        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddScoped<LoggingEmailService>();
        services.AddScoped<SmtpEmailService>();
        services.AddScoped<SendGridEmailService>();
        services.AddScoped<OutboxEmailService>();

        services.AddScoped<IEmailService>(provider => provider.GetRequiredService<OutboxEmailService>());

        services.AddScoped<LocalDiskFileStorage>();
        services.AddScoped<BlobFileStorage>();
        services.AddScoped<IFileStorage>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value;

            if (string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase))
                return provider.GetRequiredService<LocalDiskFileStorage>();

            if (string.Equals(options.Provider, "Blob", StringComparison.OrdinalIgnoreCase))
                return provider.GetRequiredService<BlobFileStorage>();

            throw new InvalidOperationException(
                $"Ogiltig filstorage-provider '{options.Provider}'. Tillatna varden ar 'Local' och 'Blob'.");
        });

        services.AddScoped<IDirectEmailSender>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;

            var backend = string.Equals(options.Provider, "outbox", StringComparison.OrdinalIgnoreCase)
                ? options.OutboxBackend
                : options.Provider;

            if (string.Equals(backend, "smtp", StringComparison.OrdinalIgnoreCase))
                return provider.GetRequiredService<SmtpEmailService>();

            if (string.Equals(backend, "sendgrid", StringComparison.OrdinalIgnoreCase))
                return provider.GetRequiredService<SendGridEmailService>();

            if (string.Equals(backend, "logging", StringComparison.OrdinalIgnoreCase))
                return provider.GetRequiredService<LoggingEmailService>();

            throw new InvalidOperationException(
                $"Ogiltig e-postprovider '{backend}'. Tillatna varden ar 'Logging', 'Smtp', 'SendGrid' och 'Outbox'.");
        });

        services.AddHostedService<OutboxProcessor>();
        services.AddScoped<DataMaintenanceCleanupService>();
        services.AddHostedService<DataMaintenanceHostedService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<EventDispatchInterceptor>();
        services.AddScoped<TenantSeedInterceptor>();

        services.AddOptions<MultitenancyOptions>()
            .Bind(configuration.GetSection(MultitenancyOptions.SectionName));

        services.AddMemoryCache();
        services.AddSingleton<IAmbientTenantContext, AmbientTenantContext>();
        services.AddScoped<ITenantContext, DefaultTenantContext>();
        services.AddDbContextFactory<TenantLookupDbContext>(options =>
            options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddSingleton<CachingTenantResolver>();
        services.AddSingleton<ITenantResolver>(provider => provider.GetRequiredService<CachingTenantResolver>());
        services.AddSingleton<ITenantResolverCacheInvalidator>(provider => provider.GetRequiredService<CachingTenantResolver>());

        services.AddDbContext<ConventionDbContext>((provider, options) =>
        {
            var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();
            var tenantSeedInterceptor = provider.GetRequiredService<TenantSeedInterceptor>();
            options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(eventDispatchInterceptor, tenantSeedInterceptor);
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

        services.AddScoped<TenantAwareUserService>();

        return services;
    }
}
