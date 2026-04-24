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
    private readonly List<Session> _sessions = [];
    private readonly List<CoOrganiser> _coOrganisers = [];
    private readonly List<CoOrganiserApplication> _coOrganiserApplications = [];
    private readonly List<EventComment> _comments = [];

    public EventId Id { get; private set; }
    public EditionId EditionId { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public PersonId LeadOrganiserId { get; private set; }
    public EventStatus Status { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ScheduleRequestText { get; private set; }
    public RegistrationType RegistrationType { get; private set; }
    public string? DropInRules { get; private set; }

    public IReadOnlyList<Session> Sessions => _sessions.AsReadOnly();
    public IReadOnlyList<CoOrganiser> CoOrganisers => _coOrganisers.AsReadOnly();
    public IReadOnlyList<CoOrganiserApplication> CoOrganiserApplications => _coOrganiserApplications.AsReadOnly();
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
        if (description.Length > 10_000)
            throw new ArgumentException("Beskrivning får inte vara längre än 10 000 tecken.", nameof(description));
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

    public void UpdateScheduleRequestText(string? scheduleRequestText)
    {
        EnsureNotCancelled();
        ScheduleRequestText = string.IsNullOrWhiteSpace(scheduleRequestText)
            ? null
            : scheduleRequestText.Trim();
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
        if (personId == LeadOrganiserId)
            throw new LeadOrganiserCannotBeCoOrganiserException();
        if (_coOrganisers.Any(c => c.PersonId == personId))
            throw new CoOrganiserAlreadyAddedException();

        var coOrganiser = new CoOrganiser(personId);
        _coOrganisers.Add(coOrganiser);
        return coOrganiser;
    }

    public CoOrganiserApplication SubmitCoOrganiserApplication(
        string email,
        string? name,
        string? message,
        PersonId requestedById,
        string? leadOrganiserEmail = null)
    {
        EnsureNotCancelled();
        if (requestedById != LeadOrganiserId)
            throw new UnauthorizedAccessException("Endast huvudarrangören kan föreslå medarrangörer.");

        var normalizedEmail = NormalizeEmail(email);
        if (leadOrganiserEmail is not null && NormalizeEmail(leadOrganiserEmail) == normalizedEmail)
            throw new LeadOrganiserCannotBeCoOrganiserException();
        if (_coOrganiserApplications.Any(a =>
                a.NormalizedEmail == normalizedEmail && a.Status == CoOrganiserApplicationStatus.Pending))
            throw new CoOrganiserApplicationAlreadyPendingException();

        var application = new CoOrganiserApplication(
            CoOrganiserApplicationId.New(),
            Id,
            email.Trim(),
            normalizedEmail,
            name,
            message,
            requestedById);
        _coOrganiserApplications.Add(application);
        RaiseDomainEvent(new CoOrganiserApplicationSubmitted(
            application.Id,
            Id,
            application.Email,
            requestedById,
            DateTimeOffset.UtcNow));
        return application;
    }

    public CoOrganiser ApproveCoOrganiserApplication(
        CoOrganiserApplicationId applicationId,
        PersonId personId,
        PersonId reviewedById)
    {
        var application = _coOrganiserApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? throw new CoOrganiserApplicationNotFoundException();

        application.Approve(personId, reviewedById);
        var coOrganiser = AddCoOrganiser(personId);
        RaiseDomainEvent(new CoOrganiserApplicationApproved(
            application.Id,
            Id,
            personId,
            reviewedById,
            DateTimeOffset.UtcNow));
        return coOrganiser;
    }

    public void RejectCoOrganiserApplication(
        CoOrganiserApplicationId applicationId,
        PersonId reviewedById,
        string? comment)
    {
        var application = _coOrganiserApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? throw new CoOrganiserApplicationNotFoundException();

        application.Reject(reviewedById, comment);
        RaiseDomainEvent(new CoOrganiserApplicationRejected(
            application.Id,
            Id,
            reviewedById,
            application.ReviewComment,
            DateTimeOffset.UtcNow));
    }

    public void CancelCoOrganiserApplication(
        CoOrganiserApplicationId applicationId,
        PersonId cancelledById)
    {
        if (cancelledById != LeadOrganiserId)
            throw new UnauthorizedAccessException("Endast huvudarrangören kan återkalla medarrangörsansökningar.");

        var application = _coOrganiserApplications.FirstOrDefault(a => a.Id == applicationId)
            ?? throw new CoOrganiserApplicationNotFoundException();

        application.Cancel();
        RaiseDomainEvent(new CoOrganiserApplicationCancelled(
            application.Id,
            Id,
            cancelledById,
            DateTimeOffset.UtcNow));
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new CoOrganiserEmailRequiredException();
        return email.Trim().ToUpperInvariant();
    }

    public bool IsOrganiser(PersonId personId)
        => LeadOrganiserId == personId || _coOrganisers.Any(c => c.PersonId == personId);
}
