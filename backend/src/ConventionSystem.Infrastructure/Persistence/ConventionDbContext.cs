using System.Reflection;
using System.Linq.Expressions;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Tenancy.Aggregates;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DomainEvent = ConventionSystem.Domain.Event.Aggregates.Event;

namespace ConventionSystem.Infrastructure.Persistence;

public sealed class ConventionDbContext(
    DbContextOptions<ConventionDbContext> options,
    ITenantContext tenantContext,
    IOptions<MultitenancyOptions> optionsAccessor) : DbContext(options)
{
    private const string TenantIdPropertyName = "TenantId";

    private Guid CurrentTenantId => tenantContext.TenantId;
    private bool IsMultitenancyEnabled => optionsAccessor.Value.Enabled;
    private bool IsSystemContext => CurrentTenantId == Guid.Empty;

    // Convention
    public DbSet<Convention> Conventions => Set<Convention>();
    public DbSet<Edition> Editions => Set<Edition>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<StaffArea> StaffAreas => Set<StaffArea>();
    public DbSet<Category> Categories => Set<Category>();

    // Event
    public DbSet<DomainEvent> Events => Set<DomainEvent>();

    // Registration
    public DbSet<VisitorRegistration> VisitorRegistrations => Set<VisitorRegistration>();
    public DbSet<SessionRegistration> SessionRegistrations => Set<SessionRegistration>();
    public DbSet<SessionWatch> SessionWatches => Set<SessionWatch>();
    public DbSet<StaffApplication> StaffApplications => Set<StaffApplication>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<PromotionCode> PromotionCodes => Set<PromotionCode>();

    // Staff
    public DbSet<Shift> Shifts => Set<Shift>();

    // Tenancy
    public DbSet<Tenant> Tenants => Set<Tenant>();

    // Infrastructure
    public DbSet<DomainEventLogEntry> DomainEventLog => Set<DomainEventLogEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ConfigureTenantScoping(modelBuilder);
    }

    private void ConfigureTenantScoping(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
                continue;

            if (entityType.GetTableName() is null)
                continue;

            if (entityType.ClrType == typeof(Tenant))
                continue;

            if (entityType.ClrType == typeof(OutboxMessage))
                continue;

            if (entityType.ClrType == typeof(DomainEventLogEntry))
                continue;

            var builder = modelBuilder.Entity(entityType.Name);
            builder.Property<Guid>(TenantIdPropertyName).HasColumnName("tenant_id");
            builder.HasIndex(TenantIdPropertyName);
            builder.HasQueryFilter(BuildTenantFilterExpression(entityType.ClrType));
        }
    }

    private LambdaExpression BuildTenantFilterExpression(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var tenantProperty = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            [typeof(Guid)],
            parameter,
            Expression.Constant(TenantIdPropertyName));

        var isMultitenancyEnabled = Expression.Property(
            Expression.Constant(this),
            nameof(IsMultitenancyEnabled));

        var isSystemContext = Expression.Property(
            Expression.Constant(this),
            nameof(IsSystemContext));

        var currentTenantId = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
        var tenantMatch = Expression.Equal(tenantProperty, currentTenantId);
        var bypassFilter = Expression.OrElse(Expression.Not(isMultitenancyEnabled), isSystemContext);
        var body = Expression.OrElse(bypassFilter, tenantMatch);

        return Expression.Lambda(body, parameter);
    }
}
