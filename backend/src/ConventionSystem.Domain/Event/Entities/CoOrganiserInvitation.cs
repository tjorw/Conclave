using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class CoOrganiserInvitation
{
    public CoOrganiserInvitationId Id { get; private set; }
    public EventId EventId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public PersonId CreatedById { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

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
        CreatedById = createdById;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
