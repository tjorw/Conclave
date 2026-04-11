using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionOpenRegistrationTests
{
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

        Assert.Throws<InvalidOperationException>(() => edition.OpenOrganiserRegistration(PersonId.New()));
    }

    [Fact]
    public void OpenOrganiserRegistration_AlreadyOpen_Throws()
    {
        var edition = CreatePublishedEdition();
        edition.OpenOrganiserRegistration(PersonId.New());

        Assert.Throws<InvalidOperationException>(() => edition.OpenOrganiserRegistration(PersonId.New()));
    }

    [Fact]
    public void OpenStaffRegistration_AlreadyOpen_Throws()
    {
        var edition = CreatePublishedEdition();
        edition.OpenStaffRegistration(PersonId.New());

        Assert.Throws<InvalidOperationException>(() => edition.OpenStaffRegistration(PersonId.New()));
    }

    [Fact]
    public void OpenVisitorRegistration_AlreadyOpen_Throws()
    {
        var edition = CreatePublishedEdition();
        edition.OpenVisitorRegistration(PersonId.New());

        Assert.Throws<InvalidOperationException>(() => edition.OpenVisitorRegistration(PersonId.New()));
    }
}
