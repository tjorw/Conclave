using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Events;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionPublishTests
{
    private static Domain.Convention.Aggregates.Edition CreateEdition()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        return convention.CreateEdition("Test", period, staff.Id, evt.Id);
    }

    [Fact]
    public void Publish_ValidEdition_TransitionsToPublished()
    {
        var edition = CreateEdition();

        edition.Publish(PersonId.New());

        Assert.Equal(EditionStatus.Published, edition.Status);
    }

    [Fact]
    public void Publish_RaisesEditionPublishedEvent()
    {
        var edition = CreateEdition();
        var performedById = PersonId.New();

        edition.Publish(performedById);

        var evt = edition.DomainEvents.OfType<EditionPublished>().Single();
        Assert.Equal(edition.Id, evt.EditionId);
        Assert.Equal(performedById, evt.PerformedById);
    }

    [Fact]
    public void Publish_AlreadyPublished_Throws()
    {
        var edition = CreateEdition();
        edition.Publish(PersonId.New());

        Assert.Throws<EditionAlreadyPublishedException>(() => edition.Publish(PersonId.New()));
    }

    [Fact]
    public void Unpublish_PublishedEdition_TransitionsToDraft()
    {
        var edition = CreateEdition();
        edition.Publish(PersonId.New());

        edition.Unpublish(PersonId.New());

        Assert.Equal(EditionStatus.Draft, edition.Status);
    }

    [Fact]
    public void Unpublish_ClosesOpenRegistrations()
    {
        var edition = CreateEdition();
        var performedById = PersonId.New();
        edition.Publish(performedById);
        edition.OpenOrganiserRegistration(performedById);
        edition.OpenStaffRegistration(performedById);
        edition.OpenVisitorRegistration(performedById);

        edition.Unpublish(performedById);

        Assert.False(edition.OrganiserRegistrationOpen);
        Assert.False(edition.StaffRegistrationOpen);
        Assert.False(edition.VisitorRegistrationOpen);
    }

    [Fact]
    public void Unpublish_RaisesEditionUnpublishedEvent()
    {
        var edition = CreateEdition();
        var performedById = PersonId.New();
        edition.Publish(performedById);
        edition.ClearDomainEvents();

        edition.Unpublish(performedById);

        var evt = edition.DomainEvents.OfType<EditionUnpublished>().Single();
        Assert.Equal(edition.Id, evt.EditionId);
        Assert.Equal(performedById, evt.PerformedById);
    }

    [Fact]
    public void Unpublish_AlreadyDraft_Throws()
    {
        var edition = CreateEdition();

        Assert.Throws<EditionAlreadyDraftException>(() => edition.Unpublish(PersonId.New()));
    }
}
