using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Tests.Convention;

public class ConventionAdministratorTests
{
    [Fact]
    public void AddAdministrator_ValidInput_AddsAdministratorAndRaisesEvent()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var existingAdmin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(existingAdmin.Id, existingAdmin.Id);
        var person = convention.CreatePerson("Anna", "anna@example.com");
        convention.ClearDomainEvents();

        convention.AddAdministrator(person.Id, existingAdmin.Id);

        Assert.True(convention.IsAdministrator(person.Id));
        var evt = convention.DomainEvents.OfType<AdministratorAdded>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(person.Id, evt.PersonId);
        Assert.Equal(existingAdmin.Id, evt.AddedById);
    }

    [Fact]
    public void RemoveAdministrator_ValidInput_RemovesAdministratorAndRaisesEvent()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var existingAdmin = convention.RegisterPerson("Admin", "admin@example.com");
        var secondAdmin = convention.CreatePerson("Anna", "anna@example.com");
        convention.AddAdministrator(existingAdmin.Id, existingAdmin.Id);
        convention.AddAdministrator(secondAdmin.Id, existingAdmin.Id);
        convention.ClearDomainEvents();

        convention.RemoveAdministrator(secondAdmin.Id, existingAdmin.Id);

        Assert.False(convention.IsAdministrator(secondAdmin.Id));
        var evt = convention.DomainEvents.OfType<AdministratorRemoved>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(secondAdmin.Id, evt.PersonId);
        Assert.Equal(existingAdmin.Id, evt.RemovedById);
    }

    [Fact]
    public void RemoveAdministrator_RemoveSelf_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        Assert.Throws<InvalidOperationException>(() => convention.RemoveAdministrator(admin.Id, admin.Id));
    }

    [Fact]
    public void RemoveAdministrator_NotAdministrator_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var existingAdmin = convention.RegisterPerson("Admin", "admin@example.com");
        var person = convention.CreatePerson("Anna", "anna@example.com");
        convention.AddAdministrator(existingAdmin.Id, existingAdmin.Id);

        Assert.Throws<InvalidOperationException>(() => convention.RemoveAdministrator(person.Id, existingAdmin.Id));
    }
}
