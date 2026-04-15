using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Entities;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
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
            throw new EventIsCancelledAndReadOnlyException();
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

    public void ChangeCategory(CategoryId newCategoryId)
    {
        EnsureNotCancelled();
        CategoryId = newCategoryId;
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
            ?? throw new SessionRequestNotFoundException();
        _sessionRequests.Remove(request);
    }

    public void SubmitForReview()
    {
        if (Status == EventStatus.Cancelled)
            throw new EventIsCancelledException();
        if (Status == EventStatus.UnderReview)
            throw new EventAlreadyUnderReviewException();
        if (string.IsNullOrWhiteSpace(Title))
            throw new EventTitleRequiredException();
        if (string.IsNullOrWhiteSpace(Description))
            throw new EventDescriptionRequiredException();

        Status = EventStatus.UnderReview;
        RaiseDomainEvent(new EventSubmittedForReview(Id, DateTimeOffset.UtcNow));
    }

    public void Approve(PersonId responsibleId)
    {
        if (Status == EventStatus.Cancelled)
            throw new EventIsCancelledException();
        if (Status == EventStatus.Published)
            throw new EventAlreadyPublishedException();
        if (string.IsNullOrWhiteSpace(Title))
            throw new EventTitleRequiredException();
        if (string.IsNullOrWhiteSpace(Description))
            throw new EventDescriptionRequiredException();

        Status = EventStatus.Published;
        RaiseDomainEvent(new EventApproved(Id, LeadOrganiserId, responsibleId, Title, DateTimeOffset.UtcNow));
    }

    public void ReturnToDraft(PersonId performedById)
    {
        if (Status == EventStatus.Draft)
            throw new EventAlreadyDraftException();

        Status = EventStatus.Draft;
    }

    public EventComment Reject(PersonId responsibleId, string comment)
    {
        if (Status != EventStatus.UnderReview)
            throw new EventNotUnderReviewException();

        Status = EventStatus.Draft;
        var eventComment = new EventComment(EventCommentId.New(), Id, responsibleId, comment, requiresHandling: false);
        _comments.Add(eventComment);

        RaiseDomainEvent(new EventRejected(Id, LeadOrganiserId, responsibleId, Title, comment, DateTimeOffset.UtcNow));
        return eventComment;
    }

    public EventComment AddOrganiserComment(PersonId organiserId, string text)
    {
        if (Status != EventStatus.Published)
            throw new EventNotPublishedException();
        if (string.IsNullOrWhiteSpace(text))
            throw new EventCommentTextRequiredException();

        var comment = new EventComment(EventCommentId.New(), Id, organiserId, text, requiresHandling: true);
        _comments.Add(comment);
        return comment;
    }

    public void RespondToComment(EventCommentId commentId, PersonId handledById, string response)
    {
        if (Status != EventStatus.Published)
            throw new EventNotPublishedException();

        var comment = _comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new EventCommentNotFoundException();

        comment.Respond(handledById, response);
    }

    public void AcknowledgeComment(EventCommentId commentId, PersonId acknowledgedById)
    {
        if (Status != EventStatus.Published)
            throw new EventNotPublishedException();

        var comment = _comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new EventCommentNotFoundException();

        if (comment.AuthorId != acknowledgedById)
            throw new EventCommentAcknowledgeMustBeDoneByAuthorException();

        comment.Acknowledge(acknowledgedById);
    }

    public void CancelEvent(PersonId responsibleId)
    {
        if (Status == EventStatus.Cancelled)
            throw new EventAlreadyCancelledException();

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
            ?? throw new SessionNotFoundException();

        session.Update(venueId, timeSlot, maxSeats, startType);
    }

    public void DeactivateSession(SessionId sessionId, PersonId performedById)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId)
            ?? throw new SessionNotFoundException();

        session.Deactivate();
        RaiseDomainEvent(new SessionDeactivated(sessionId, Id, performedById, DateTimeOffset.UtcNow));
    }

    public CoOrganiser AddCoOrganiser(PersonId personId)
    {
        if (_coOrganisers.Any(c => c.PersonId == personId))
            throw new CoOrganiserAlreadyAddedException();

        var coOrganiser = new CoOrganiser(personId);
        _coOrganisers.Add(coOrganiser);
        return coOrganiser;
    }

    public bool IsOrganiser(PersonId personId)
        => LeadOrganiserId == personId || _coOrganisers.Any(c => c.PersonId == personId);
}
