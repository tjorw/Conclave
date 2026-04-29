using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class CoOrganiserInvitation
{
    public CoOrganiserInvitationId Id { get; private set; }
    public EventId EventId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public CoOrganiserInvitationStatus Status { get; private set; }
    public PersonId CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public PersonId? RedeemedById { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }
    public PersonId? CancelledById { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    private CoOrganiserInvitation() { }

    internal CoOrganiserInvitation(
        CoOrganiserInvitationId id,
        EventId eventId,
        string email,
        string normalizedEmail,
        string code,
        PersonId createdById)
    {
        Id = id;
        EventId = eventId;
        Email = email;
        NormalizedEmail = normalizedEmail;
        Code = code;
        Status = CoOrganiserInvitationStatus.Active;
        CreatedById = createdById;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    internal void Redeem(PersonId redeemedById)
    {
        EnsureActive();
        Status = CoOrganiserInvitationStatus.Redeemed;
        RedeemedById = redeemedById;
        RedeemedAt = DateTimeOffset.UtcNow;
    }

    internal void Cancel(PersonId cancelledById)
    {
        EnsureActive();
        Status = CoOrganiserInvitationStatus.Cancelled;
        CancelledById = cancelledById;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    private void EnsureActive()
    {
        if (Status != CoOrganiserInvitationStatus.Active)
            throw new CoOrganiserInvitationNotActiveException();
    }
}
