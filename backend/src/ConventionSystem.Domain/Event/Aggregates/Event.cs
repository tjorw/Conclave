using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Entities;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;

namespace ConventionSystem.Domain.Event.Aggregates;

public sealed class Event : AggregateRoot
{
    private readonly List<SessionRequest> _sessionRequests = [];
    private readonly List<Session> _sessions = [];
    private readonly List<CoOrganiser> _coOrganisers = [];
    private readonly List<EventComment> _comments = [];

    public EventId Id { get; private set; }
    public EditionId EditionId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public PersonId LeadOrganiserId { get; private set; }
    public EventStatus Status { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RegistrationType RegistrationType { get; private set; }
    public string? DropInRules { get; private set; }

    public IReadOnlyList<SessionRequest> SessionRequests => _sessionRequests.AsReadOnly();
    public IReadOnlyList<Session> Sessions => _sessions.AsReadOnly();
    public IReadOnlyList<CoOrganiser> CoOrganisers => _coOrganisers.AsReadOnly();
    public IReadOnlyList<EventComment> Comments => _comments.AsReadOnly();

    private Event() { }

    public Event(EventId id, EditionId editionId, CategoryId categoryId, PersonId leadOrganiserId)
    {
        Id = id;
        EditionId = editionId;
        CategoryId = categoryId;
        LeadOrganiserId = leadOrganiserId;
        Status = EventStatus.Draft;

        RaiseDomainEvent(new EventCreated(id, editionId, categoryId, leadOrganiserId, DateTimeOffset.UtcNow));
    }

    private void EnsureNotCancelled()
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidOperationException("Evenemanget är inställt och kan inte redigeras.");
    }

    public void EditTitle(string title)
    {
        EnsureNotCancelled();
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Titel får inte vara tom.", nameof(title));
        Title = title;
    }

    public void EditDescription(string description)
    {
        EnsureNotCancelled();
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Beskrivning får inte vara tom.", nameof(description));
        Description = description;
    }

    public void SetRegistrationType(RegistrationType registrationType, string? dropInRules = null)
    {
        EnsureNotCancelled();
        RegistrationType = registrationType;
        DropInRules = dropInRules;
    }

    public SessionRequest AddSessionRequest(string description, int durationMinutes, int seats, StartType startType)
    {
        EnsureNotCancelled();
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration måste vara mer än 0 minuter.", nameof(durationMinutes));
        var request = new SessionRequest(SessionRequestId.New(), description, durationMinutes, seats, startType);
        _sessionRequests.Add(request);
        return request;
    }

    public void RemoveSessionRequest(SessionRequestId requestId)
    {
        EnsureNotCancelled();
        var request = _sessionRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Sessionönskemålet hittades inte.");
        _sessionRequests.Remove(request);
    }

    public void SubmitForReview()
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidOperationException("Evenemanget är inställt.");
        if (Status == EventStatus.UnderReview)
            throw new InvalidOperationException("Evenemanget är redan under granskning.");
        if (string.IsNullOrWhiteSpace(Title))
            throw new InvalidOperationException("Evenemanget måste ha en titel.");
        if (string.IsNullOrWhiteSpace(Description))
            throw new InvalidOperationException("Evenemanget måste ha en beskrivning.");

        Status = EventStatus.UnderReview;
        RaiseDomainEvent(new EventSubmittedForReview(Id, DateTimeOffset.UtcNow));
    }

    public void Approve(PersonId responsibleId)
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidOperationException("Evenemanget är inställt.");
        if (Status == EventStatus.Published)
            throw new InvalidOperationException("Evenemanget är redan publicerat.");
        if (string.IsNullOrWhiteSpace(Title))
            throw new InvalidOperationException("Evenemanget måste ha en titel.");
        if (string.IsNullOrWhiteSpace(Description))
            throw new InvalidOperationException("Evenemanget måste ha en beskrivning.");

        Status = EventStatus.Published;
        RaiseDomainEvent(new EventApproved(Id, LeadOrganiserId, responsibleId, Title, DateTimeOffset.UtcNow));
    }

    public void ReturnToDraft(PersonId performedById)
    {
        if (Status == EventStatus.Draft)
            throw new InvalidOperationException("Evenemanget är redan i utkastläge.");

        Status = EventStatus.Draft;
    }

    public EventComment Reject(PersonId responsibleId, string comment)
    {
        if (Status != EventStatus.UnderReview)
            throw new InvalidOperationException("Evenemanget är inte under granskning.");

        Status = EventStatus.Draft;
        var eventComment = new EventComment(EventCommentId.New(), Id, responsibleId, comment);
        _comments.Add(eventComment);

        RaiseDomainEvent(new EventRejected(Id, LeadOrganiserId, responsibleId, Title, comment, DateTimeOffset.UtcNow));
        return eventComment;
    }

    public void CancelEvent(PersonId responsibleId)
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidOperationException("Evenemanget är redan inställt.");

        Status = EventStatus.Cancelled;
        RaiseDomainEvent(new EventCancelled(Id, responsibleId, DateTimeOffset.UtcNow));
    }

    public Session CreateSession(VenueId venueId, TimeSlot timeSlot, int maxSeats, StartType startType)
    {
        var session = new Session(SessionId.New(), Id, venueId, timeSlot, maxSeats, startType);
        _sessions.Add(session);
        RaiseDomainEvent(new SessionCreated(Id, session.Id, venueId, DateTimeOffset.UtcNow));
        return session;
    }

    public void UpdateSession(SessionId sessionId, VenueId venueId, TimeSlot timeSlot, int maxSeats, StartType startType, PersonId performedById)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("Sessionen hittades inte.");

        session.Update(venueId, timeSlot, maxSeats, startType);
    }

    public void DeactivateSession(SessionId sessionId, PersonId performedById)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("Sessionen hittades inte.");

        session.Deactivate();
        RaiseDomainEvent(new SessionDeactivated(sessionId, Id, performedById, DateTimeOffset.UtcNow));
    }

    public CoOrganiser AddCoOrganiser(PersonId personId)
    {
        if (_coOrganisers.Any(c => c.PersonId == personId))
            throw new InvalidOperationException("Personen är redan medarrangör för detta evenemang.");

        var coOrganiser = new CoOrganiser(personId);
        _coOrganisers.Add(coOrganiser);
        return coOrganiser;
    }
}
