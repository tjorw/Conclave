using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionOpenRegistrationTests
{
    [Fact]
    public void CloseOrganiserRegistration_ClearsFlag()
    {
        var edition = CreatePublishedEdition();
        edition.OpenOrganiserRegistration(PersonId.New());

        edition.CloseOrganiserRegistration(PersonId.New());

        Assert.False(edition.OrganiserRegistrationOpen);
    }

    [Fact]
    public void CloseStaffRegistration_ClearsFlag()
    {
        var edition = CreatePublishedEdition();
        edition.OpenStaffRegistration(PersonId.New());

        edition.CloseStaffRegistration(PersonId.New());

        Assert.False(edition.StaffRegistrationOpen);
    }

    [Fact]
    public void CloseVisitorRegistration_ClearsFlag()
    {
        var edition = CreatePublishedEdition();
        edition.OpenVisitorRegistration(PersonId.New());

        edition.CloseVisitorRegistration(PersonId.New());

        Assert.False(edition.VisitorRegistrationOpen);
    }

    [Fact]
    public void CloseOrganiserRegistration_RaisesRegistrationClosedEvent()
    {
        var edition = CreatePublishedEdition();
        var performedById = PersonId.New();
        edition.OpenOrganiserRegistration(performedById);
        edition.ClearDomainEvents();

        edition.CloseOrganiserRegistration(performedById);

        var domainEvent = edition.DomainEvents.OfType<RegistrationClosed>().Single();
        Assert.Equal(edition.Id, domainEvent.EditionId);
        Assert.Equal(RegistrationType.Organiser, domainEvent.Type);
    }

    [Fact]
    public void CloseStaffRegistration_RaisesRegistrationClosedEvent()
    {
        var edition = CreatePublishedEdition();
        var performedById = PersonId.New();
        edition.OpenStaffRegistration(performedById);
        edition.ClearDomainEvents();

        edition.CloseStaffRegistration(performedById);

        var domainEvent = edition.DomainEvents.OfType<RegistrationClosed>().Single();
        Assert.Equal(edition.Id, domainEvent.EditionId);
        Assert.Equal(RegistrationType.Staff, domainEvent.Type);
    }

    [Fact]
    public void CloseVisitorRegistration_RaisesRegistrationClosedEvent()
    {
        var edition = CreatePublishedEdition();
        var performedById = PersonId.New();
        edition.OpenVisitorRegistration(performedById);
        edition.ClearDomainEvents();

        edition.CloseVisitorRegistration(performedById);

        var domainEvent = edition.DomainEvents.OfType<RegistrationClosed>().Single();
        Assert.Equal(edition.Id, domainEvent.EditionId);
        Assert.Equal(RegistrationType.Visitor, domainEvent.Type);
    }

    [Fact]
    public void CloseOrganiserRegistration_NotOpen_Throws()
    {
        var edition = CreatePublishedEdition();

        Assert.Throws<OrganiserRegistrationNotOpenException>(() => edition.CloseOrganiserRegistration(PersonId.New()));
    }

    [Fact]
    public void CloseStaffRegistration_NotOpen_Throws()
    {
        var edition = CreatePublishedEdition();

        Assert.Throws<StaffRegistrationNotOpenException>(() => edition.CloseStaffRegistration(PersonId.New()));
    }

    [Fact]
    public void CloseVisitorRegistration_NotOpen_Throws()
    {
        var edition = CreatePublishedEdition();

        Assert.Throws<VisitorRegistrationNotOpenException>(() => edition.CloseVisitorRegistration(PersonId.New()));
    }


    private static Domain.Convention.Aggregates.Edition CreatePublishedEdition()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test", period, staff.Id, evt.Id);
        edition.Publish(PersonId.New());
        edition.ClearDomainEvents();
        return edition;
    }

    [Fact]
    public void OpenOrganiserRegistration_SetsFlag()
    {
        var edition = CreatePublishedEdition();
        edition.OpenOrganiserRegistration(PersonId.New());
        Assert.True(edition.OrganiserRegistrationOpen);
    }

    [Fact]
    public void OpenStaffRegistration_SetsFlag()
    {
        var edition = CreatePublishedEdition();
        edition.OpenStaffRegistration(PersonId.New());
        Assert.True(edition.StaffRegistrationOpen);
    }

    [Fact]
    public void OpenVisitorRegistration_SetsFlag()
    {
        var edition = CreatePublishedEdition();
        edition.OpenVisitorRegistration(PersonId.New());
        Assert.True(edition.VisitorRegistrationOpen);
    }

    [Theory]
    [InlineData(RegistrationType.Organiser)]
    [InlineData(RegistrationType.Staff)]
    [InlineData(RegistrationType.Visitor)]
    public void OpenRegistration_RaisesRegistrationOpenedEvent(RegistrationType type)
    {
        var edition = CreatePublishedEdition();
        var performedById = PersonId.New();

        if (type == RegistrationType.Organiser) edition.OpenOrganiserRegistration(performedById);
        else if (type == RegistrationType.Staff) edition.OpenStaffRegistration(performedById);
        else edition.OpenVisitorRegistration(performedById);

        var domainEvent = edition.DomainEvents.OfType<RegistrationOpened>().Single();
        Assert.Equal(edition.Id, domainEvent.EditionId);
        Assert.Equal(type, domainEvent.Type);
        Assert.Equal(performedById, domainEvent.PerformedById);
    }

    [Fact]
    public void OpenOrganiserRegistration_DraftEdition_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test", period, staff.Id, evt.Id);

        Assert.Throws<EditionMustBePublishedException>(() => edition.OpenOrganiserRegistration(PersonId.New()));
    }

    [Fact]
    public void OpenOrganiserRegistration_AlreadyOpen_Throws()
    {
        var edition = CreatePublishedEdition();
        edition.OpenOrganiserRegistration(PersonId.New());

        Assert.Throws<OrganiserRegistrationAlreadyOpenException>(() => edition.OpenOrganiserRegistration(PersonId.New()));
    }

    [Fact]
    public void OpenStaffRegistration_AlreadyOpen_Throws()
    {
        var edition = CreatePublishedEdition();
        edition.OpenStaffRegistration(PersonId.New());

        Assert.Throws<StaffRegistrationAlreadyOpenException>(() => edition.OpenStaffRegistration(PersonId.New()));
    }

    [Fact]
    public void OpenVisitorRegistration_AlreadyOpen_Throws()
    {
        var edition = CreatePublishedEdition();
        edition.OpenVisitorRegistration(PersonId.New());

        Assert.Throws<VisitorRegistrationAlreadyOpenException>(() => edition.OpenVisitorRegistration(PersonId.New()));
    }
}
