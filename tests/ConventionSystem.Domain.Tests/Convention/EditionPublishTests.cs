using ConventionSystem.Domain.Convention.Enums;
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

        Assert.Throws<InvalidOperationException>(() => edition.Publish(PersonId.New()));
    }
}
