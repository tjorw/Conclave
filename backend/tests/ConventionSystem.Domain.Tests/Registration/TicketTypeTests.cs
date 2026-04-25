using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Tests.Registration;

public class TicketTypeTests
{
    private static TicketType CreateTicketType(
        IReadOnlyList<DateOnly>? validDays = null,
        Guid[]? allowedCategories = null,
        string? description = null)
        => new(TicketTypeId.New(), EditionId.New(), "Helgbiljett", 15000, TicketTypeCategory.Visitor,
            validDays, allowedCategories, description);

    [Fact]
    public void Constructor_NoValidDays_ValidDaysIsNull()
    {
        var tt = CreateTicketType();

        Assert.Null(tt.ValidDays);
        Assert.Null(tt.AllowedCategories);
    }

    [Fact]
    public void Constructor_WithValidDays_StoresValidDays()
    {
        var days = new[] { new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 2) };

        var tt = CreateTicketType(validDays: days);

        Assert.Equal(days, tt.ValidDays);
    }

    [Fact]
    public void Constructor_WithAllowedCategories_StoresCategories()
    {
        var catIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var tt = CreateTicketType(allowedCategories: catIds);

        Assert.Equal(catIds, tt.AllowedCategories);
    }

    [Fact]
    public void Constructor_WithDescription_StoresTrimmedMarkdown()
    {
        var tt = CreateTicketType(description: "  - T-shirt\n- Matkupong  ");

        Assert.Equal("- T-shirt\n- Matkupong", tt.Description);
    }

    [Fact]
    public void Constructor_NegativePrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TicketType(TicketTypeId.New(), EditionId.New(), "Biljett", -1, TicketTypeCategory.Visitor));
    }

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TicketType(TicketTypeId.New(), EditionId.New(), "", 0, TicketTypeCategory.Visitor));
    }

    [Fact]
    public void Update_ChangesNamePriceTypeAndDays()
    {
        var tt = CreateTicketType();
        var newDays = new[] { new DateOnly(2027, 3, 3) };
        var newCats = new[] { Guid.NewGuid() };

        tt.Update("Ny biljett", 20000, TicketTypeCategory.Staff, newDays, newCats, "## Beskrivning");

        Assert.Equal("Ny biljett", tt.Name);
        Assert.Equal(20000, tt.Price);
        Assert.Equal(TicketTypeCategory.Staff, tt.Type);
        Assert.Equal(newDays, tt.ValidDays);
        Assert.Equal(newCats, tt.AllowedCategories);
        Assert.Equal("## Beskrivning", tt.Description);
    }

    [Fact]
    public void Update_NullValidDays_ClearsValidDays()
    {
        var tt = CreateTicketType(validDays: [new DateOnly(2027, 3, 1)]);

        tt.Update("Biljett", 0, TicketTypeCategory.Visitor, null, null, null);

        Assert.Null(tt.ValidDays);
    }
}
