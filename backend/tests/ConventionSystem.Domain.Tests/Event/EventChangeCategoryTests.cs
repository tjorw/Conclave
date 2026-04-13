using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Aggregates;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Tests.Event;

public class EventChangeCategoryTests
{
    private static Domain.Event.Aggregates.Event CreateDraftEvent(CategoryId? categoryId = null)
        => new(EventId.New(), EditionId.New(), categoryId ?? CategoryId.New(), PersonId.New());

    [Fact]
    public void ChangeCategory_UpdatesCategoryId()
    {
        var oldCategory = CategoryId.New();
        var newCategory = CategoryId.New();
        var ev = CreateDraftEvent(oldCategory);

        ev.ChangeCategory(newCategory);

        Assert.Equal(newCategory, ev.CategoryId);
    }

    [Fact]
    public void ChangeCategory_WhenCancelled_Throws()
    {
        var ev = CreateDraftEvent();
        ev.CancelEvent(PersonId.New());

        Assert.Throws<InvalidOperationException>(() => ev.ChangeCategory(CategoryId.New()));
    }

    [Fact]
    public void ChangeCategory_WhenPublished_Succeeds()
    {
        var ev = CreateDraftEvent();
        ev.EditTitle("Titel");
        ev.EditDescription("Beskrivning");
        ev.Approve(PersonId.New());
        var newCategory = CategoryId.New();

        ev.ChangeCategory(newCategory);

        Assert.Equal(newCategory, ev.CategoryId);
    }
}
