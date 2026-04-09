using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Tests.Convention;

public class ConventionPersonTests
{
    private static Domain.Convention.Aggregates.Convention CreateConvention() =>
        new(ConventionId.New(), "Test Con", "test-con");

    // --- CreatePerson ---

    [Fact]
    public void CreatePerson_ValidInput_ReturnsPerson()
    {
        var convention = CreateConvention();

        var person = convention.CreatePerson("Anna", "anna@example.com");

        Assert.Equal("Anna", person.Name);
        Assert.Equal("anna@example.com", person.Email);
        Assert.True(person.IsActive);
        Assert.Equal(convention.Id, person.ConventionId);
    }

    [Fact]
    public void CreatePerson_RaisesPersonCreatedEvent()
    {
        var convention = CreateConvention();
        convention.ClearDomainEvents();

        var person = convention.CreatePerson("Anna", "anna@example.com");

        var evt = convention.DomainEvents.OfType<PersonCreated>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(person.Id, evt.PersonId);
        Assert.Equal(convention.Id, evt.ConventionId);
    }

    // --- UpdatePersonDetails ---

    [Fact]
    public void UpdatePersonDetails_ValidInput_UpdatesPerson()
    {
        var convention = CreateConvention();
        var person = convention.CreatePerson("Anna", "anna@example.com", "070-111");
        convention.ClearDomainEvents();

        convention.UpdatePersonDetails(person, "Anna Svensson", "anna.svensson@example.com", "070-222");

        Assert.Equal("Anna Svensson", person.Name);
        Assert.Equal("anna.svensson@example.com", person.Email);
        Assert.Equal("070-222", person.Phone);
    }

    [Fact]
    public void UpdatePersonDetails_RaisesPersonUpdatedEvent()
    {
        var convention = CreateConvention();
        var person = convention.CreatePerson("Anna", "anna@example.com");
        convention.ClearDomainEvents();

        convention.UpdatePersonDetails(person, "Anna S", "annas@example.com", null);

        var evt = convention.DomainEvents.OfType<PersonUpdated>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(person.Id, evt.PersonId);
    }

    [Fact]
    public void UpdatePersonDetails_PersonFromOtherConvention_Throws()
    {
        var convention = CreateConvention();
        var otherConvention = CreateConvention();
        var person = otherConvention.CreatePerson("Anna", "anna@example.com");

        Assert.Throws<InvalidOperationException>(
            () => convention.UpdatePersonDetails(person, "Anna", "anna@example.com", null));
    }

    // --- DeactivatePerson ---

    [Fact]
    public void DeactivatePerson_ActivePerson_DeactivatesIt()
    {
        var convention = CreateConvention();
        var person = convention.CreatePerson("Anna", "anna@example.com");
        convention.ClearDomainEvents();

        convention.DeactivatePerson(person);

        Assert.False(person.IsActive);
    }

    [Fact]
    public void DeactivatePerson_RaisesPersonDeactivatedEvent()
    {
        var convention = CreateConvention();
        var person = convention.CreatePerson("Anna", "anna@example.com");
        convention.ClearDomainEvents();

        convention.DeactivatePerson(person);

        var evt = convention.DomainEvents.OfType<PersonDeactivated>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(person.Id, evt.PersonId);
    }

    [Fact]
    public void DeactivatePerson_AlreadyInactive_Throws()
    {
        var convention = CreateConvention();
        var person = convention.CreatePerson("Anna", "anna@example.com");
        convention.DeactivatePerson(person);

        Assert.Throws<InvalidOperationException>(() => convention.DeactivatePerson(person));
    }

    [Fact]
    public void DeactivatePerson_PersonFromOtherConvention_Throws()
    {
        var convention = CreateConvention();
        var otherConvention = CreateConvention();
        var person = otherConvention.CreatePerson("Anna", "anna@example.com");

        Assert.Throws<InvalidOperationException>(() => convention.DeactivatePerson(person));
    }
}
