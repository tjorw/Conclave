using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Exceptions;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Tests.Content;

public sealed class PageMenuSortOrderTests
{
    [Fact]
    public void Constructor_SetsDefaultMenuSortOrderToZero()
    {
        var page = CreatePage();

        Assert.Equal(0, page.MenuSortOrder);
    }

    [Fact]
    public void SetMenuSortOrder_WithNonNegativeValue_UpdatesSortOrder()
    {
        var page = CreatePage();

        page.SetMenuSortOrder(3);

        Assert.Equal(3, page.MenuSortOrder);
    }

    [Fact]
    public void SetMenuSortOrder_WithNegativeValue_Throws()
    {
        var page = CreatePage();

        Assert.Throws<PageMenuSortOrderMustBeNonNegativeException>(() => page.SetMenuSortOrder(-1));
    }

    private static Page CreatePage()
        => new(
            PageId.New(),
            ConventionId.New(),
            null,
            "info",
            "Info",
            "Text",
            showInPublicMenu: true);
}
