using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Entities;

public sealed class TicketType : AggregateRoot
{
    private readonly List<TicketPerk> _perks = [];

    public TicketTypeId Id { get; private set; }
    public EditionId EditionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Price { get; private set; }
    public TicketTypeCategory Type { get; private set; }
    public bool IsSellable { get; private set; }
    public bool IsPubliclyVisible { get; private set; }

    public IReadOnlyList<TicketPerk> Perks => _perks.AsReadOnly();

    private TicketType() { }

    public TicketType(TicketTypeId id, EditionId editionId, string name, int price, TicketTypeCategory type,
        bool isSellable, bool isPubliclyVisible)
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
        IsSellable = isSellable;
        IsPubliclyVisible = isPubliclyVisible;
    }

    public void Update(string name, int price, bool isSellable, bool isPubliclyVisible)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Namn får inte vara tomt.", nameof(name));
        if (price < 0)
            throw new ArgumentException("Pris får inte vara negativt.", nameof(price));

        Name = name;
        Price = price;
        IsSellable = isSellable;
        IsPubliclyVisible = isPubliclyVisible;
    }

    public TicketPerk AddPerk(string description)
    {
        var perk = new TicketPerk(TicketPerkId.New(), description);
        _perks.Add(perk);
        return perk;
    }

    public void RemovePerk(TicketPerkId perkId)
    {
        var perk = _perks.FirstOrDefault(p => p.Id == perkId)
            ?? throw new TicketPerkNotFoundException();
        _perks.Remove(perk);
    }
}
