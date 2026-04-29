namespace ConventionSystem.Domain.Event.Ids;

public readonly record struct CoOrganiserInvitationId(Guid Value)
{
    public static CoOrganiserInvitationId New() => new(Guid.NewGuid());
}
