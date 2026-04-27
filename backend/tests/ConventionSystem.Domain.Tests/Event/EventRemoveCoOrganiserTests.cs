using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.Exceptions;

namespace ConventionSystem.Domain.Tests.Event;

public class EventRemoveCoOrganiserTests
{
    private static Domain.Event.Aggregates.Event CreateEvent()
        => new(EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());

    [Fact]
    public void RemoveCoOrganiser_ExistingCoOrganiser_RemovesFromList()
    {
        var ev = CreateEvent();
        var coOrganiserId = PersonId.New();
        ev.AddCoOrganiser(coOrganiserId);

        ev.RemoveCoOrganiser(coOrganiserId, PersonId.New());

        Assert.Empty(ev.CoOrganisers);
    }

    [Fact]
    public void RemoveCoOrganiser_NonExistingCoOrganiser_ThrowsCoOrganiserNotFoundException()
    {
        var ev = CreateEvent();

        Assert.Throws<CoOrganiserNotFoundException>(() =>
            ev.RemoveCoOrganiser(PersonId.New(), PersonId.New()));
    }

    [Fact]
    public void RemoveCoOrganiser_OneOfManyCoOrganisers_RemovesOnlyTargeted()
    {
        var ev = CreateEvent();
        var first = PersonId.New();
        var second = PersonId.New();
        ev.AddCoOrganiser(first);
        ev.AddCoOrganiser(second);

        ev.RemoveCoOrganiser(first, PersonId.New());

        Assert.Single(ev.CoOrganisers);
        Assert.Equal(second, ev.CoOrganisers[0].PersonId);
    }
}
