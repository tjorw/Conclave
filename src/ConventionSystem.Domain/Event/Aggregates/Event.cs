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
    private readonly List<EventVersion> _versions = [];
    private readonly List<Session> _sessions = [];
    private readonly List<CoOrganiser> _coOrganisers = [];
    private readonly List<EventComment> _comments = [];

    public EventId Id { get; private set; }
    public EditionId EditionId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public PersonId LeadOrganiserId { get; private set; }
    public EventVersionId? PublishedVersionId { get; private set; }
    public EventVersionId? DraftVersionId { get; private set; }
    public EventStatus Status { get; private set; }

    public IReadOnlyList<EventVersion> Versions => _versions.AsReadOnly();
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

        var initialVersion = new EventVersion(EventVersionId.New(), id);
        _versions.Add(initialVersion);
        DraftVersionId = initialVersion.Id;

        RaiseDomainEvent(new EventCreated(id, editionId, categoryId, leadOrganiserId, DateTimeOffset.UtcNow));
    }

    public EventVersion GetDraftVersion()
    {
        if (DraftVersionId is null)
            throw new InvalidOperationException("Inga utkast finns för detta evenemang.");
        return _versions.First(v => v.Id == DraftVersionId.Value);
    }

    public EventVersion? GetPublishedVersion() =>
        PublishedVersionId is null ? null : _versions.FirstOrDefault(v => v.Id == PublishedVersionId.Value);

    public void SubmitForReview()
    {
        if (Status != EventStatus.Draft)
            throw new InvalidOperationException("Evenemanget måste vara i utkastläge för att skickas in för granskning.");

        var draft = GetDraftVersion();
        if (string.IsNullOrWhiteSpace(draft.Title))
            throw new InvalidOperationException("Evenemanget måste ha en titel innan det kan skickas in för granskning.");
        if (string.IsNullOrWhiteSpace(draft.Description))
            throw new InvalidOperationException("Evenemanget måste ha en beskrivning innan det kan skickas in för granskning.");

        draft.SubmitForReview();
        Status = EventStatus.UnderReview;
        RaiseDomainEvent(new EventSubmittedForReview(Id, draft.Id, DateTimeOffset.UtcNow));
    }

    public void ApproveVersion(PersonId responsibleId)
    {
        if (Status != EventStatus.UnderReview)
            throw new InvalidOperationException("Evenemanget är inte under granskning.");

        var draft = GetDraftVersion();
        draft.Approve();
        PublishedVersionId = draft.Id;
        DraftVersionId = null;
        Status = EventStatus.Published;

        RaiseDomainEvent(new VersionApproved(Id, draft.Id, LeadOrganiserId, responsibleId, draft.Title, DateTimeOffset.UtcNow));
    }

    public void RejectVersion(PersonId responsibleId, string comment)
    {
        if (Status != EventStatus.UnderReview)
            throw new InvalidOperationException("Evenemanget är inte under granskning.");

        var rejected = GetDraftVersion();
        rejected.Reject();

        _comments.Add(new EventComment(EventCommentId.New(), Id, rejected.Id, responsibleId, comment));

        // Nytt utkast med kopierat innehåll så arrangören inte behöver börja om från noll
        var newDraft = new EventVersion(EventVersionId.New(), Id,
            rejected.Title, rejected.Description, rejected.RegistrationType, rejected.DropInRules);
        _versions.Add(newDraft);
        DraftVersionId = newDraft.Id;
        Status = EventStatus.Draft;

        RaiseDomainEvent(new VersionRejected(Id, rejected.Id, LeadOrganiserId, responsibleId, rejected.Title, comment, DateTimeOffset.UtcNow));
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

    public void AddComment(PersonId authorId, string text, EventVersionId? versionId = null)
    {
        _comments.Add(new EventComment(EventCommentId.New(), Id, versionId, authorId, text));
    }
}
