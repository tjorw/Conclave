using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Tests.Event;

public sealed class EventFeaturedTests
{
    private static ConventionSystem.Domain.Event.Aggregates.Event CreateDraftEvent()
        => new(
            EventId.New(),
            EditionId.New(),
            CategoryId.New(),
            PersonId.New());

    [Fact]
    public void SetFeatured_WhenEnabled_SetsFlagAndSortOrder()
    {
        var ev = CreateDraftEvent();

        ev.SetFeatured(true, 2);

        Assert.True(ev.IsFeatured);
        Assert.Equal(2, ev.FeaturedSortOrder);
    }

    [Fact]
    public void SetFeatured_WhenDisabled_ClearsSortOrder()
    {
        var ev = CreateDraftEvent();
        ev.SetFeatured(true, 2);

        ev.SetFeatured(false, null);

        Assert.False(ev.IsFeatured);
        Assert.Null(ev.FeaturedSortOrder);
    }

    [Fact]
    public void SetFeatured_WithoutSortOrder_Throws()
    {
        var ev = CreateDraftEvent();

        Assert.Throws<EventFeaturedSortOrderRequiredException>(() => ev.SetFeatured(true, null));
    }

    [Fact]
    public void SetFeatured_WithNegativeSortOrder_Throws()
    {
        var ev = CreateDraftEvent();

        Assert.Throws<EventFeaturedSortOrderMustBeNonNegativeException>(() => ev.SetFeatured(true, -1));
    }
}
