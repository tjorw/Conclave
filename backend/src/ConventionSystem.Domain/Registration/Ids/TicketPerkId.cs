namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct TicketPerkId(Guid Value)
{
    public static TicketPerkId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
