namespace ConventionSystem.Domain.Registration.Ids;

public readonly record struct TicketTypeId(Guid Value)
{
    public static TicketTypeId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
