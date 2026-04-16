using System.Reflection;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Staff.Aggregates;
using Microsoft.EntityFrameworkCore;
using DomainEvent = ConventionSystem.Domain.Event.Aggregates.Event;

namespace ConventionSystem.Infrastructure.Persistence;

public sealed class ConventionDbContext(DbContextOptions<ConventionDbContext> options) : DbContext(options)
{
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

    // Staff
    public DbSet<Shift> Shifts => Set<Shift>();

    // Infrastructure
    public DbSet<DomainEventLogEntry> DomainEventLog => Set<DomainEventLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
