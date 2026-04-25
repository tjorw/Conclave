using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Entities;

public sealed class TicketType : AggregateRoot
{
    public TicketTypeId Id { get; private set; }
    public EditionId EditionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Price { get; private set; }
    public TicketTypeCategory Type { get; private set; }
    public IReadOnlyList<DateOnly>? ValidDays { get; private set; }
    public Guid[]? AllowedCategories { get; private set; }
    public string? Description { get; private set; }

    private TicketType() { }

    public TicketType(TicketTypeId id, EditionId editionId, string name, int price, TicketTypeCategory type,
        IReadOnlyList<DateOnly>? validDays = null, Guid[]? allowedCategories = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        if (price < 0)
            throw new ArgumentException("Pris får inte vara negativt.", nameof(price));

        Id = id;
        EditionId = editionId;
        Name = name;
        Price = price;
        Type = type;
        ValidDays = validDays;
        AllowedCategories = allowedCategories;
        Description = NormalizeDescription(description);
    }

    public void Update(
        string name,
        int price,
        TicketTypeCategory type,
        IReadOnlyList<DateOnly>? validDays,
        Guid[]? allowedCategories,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        if (price < 0)
            throw new ArgumentException("Pris får inte vara negativt.", nameof(price));

        Name = name;
        Price = price;
        Type = type;
        ValidDays = validDays;
        AllowedCategories = allowedCategories;
        Description = NormalizeDescription(description);
    }

    private static string? NormalizeDescription(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
