using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class EventVersion : Entity<EventVersionId>
{
    private readonly List<SessionRequest> _sessionRequests = [];

    public EventId EventId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RegistrationType RegistrationType { get; private set; }
    public string? DropInRules { get; private set; }
    public VersionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<SessionRequest> SessionRequests => _sessionRequests.AsReadOnly();

    private EventVersion() { }

    internal EventVersion(EventVersionId id, EventId eventId)
        : base(id)
    {
        EventId = eventId;
        Status = VersionStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    // Används vid skapande av nytt utkast efter avvisning – kopierar innehåll från föregående version
    internal EventVersion(EventVersionId id, EventId eventId, string title, string description,
        RegistrationType registrationType, string? dropInRules)
        : base(id)
    {
        EventId = eventId;
        Title = title;
        Description = description;
        RegistrationType = registrationType;
        DropInRules = dropInRules;
        Status = VersionStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void EditTitle(string title)
    {
        EnsureDraft();
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Titel får inte vara tom.", nameof(title));
        Title = title;
    }

    public void EditDescription(string description)
    {
        EnsureDraft();
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Beskrivning får inte vara tom.", nameof(description));
        Description = description;
    }

    public void SetRegistrationType(RegistrationType registrationType, string? dropInRules = null)
    {
        EnsureDraft();
        RegistrationType = registrationType;
        DropInRules = dropInRules;
    }

    public SessionRequest AddSessionRequest(string description, int durationMinutes, int seats, StartType startType)
    {
        EnsureDraft();
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration måste vara mer än 0 minuter.", nameof(durationMinutes));
        var request = new SessionRequest(SessionRequestId.New(), description, durationMinutes, seats, startType);
        _sessionRequests.Add(request);
        return request;
    }

    public void RemoveSessionRequest(SessionRequestId requestId)
    {
        EnsureDraft();
        var request = _sessionRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Sessionönskemålet hittades inte.");
        _sessionRequests.Remove(request);
    }

    internal void SubmitForReview()
    {
        if (Status != VersionStatus.Draft)
            throw new InvalidOperationException("Versionen måste vara ett utkast för att skickas in.");
        Status = VersionStatus.UnderReview;
    }

    internal void Approve()
    {
        if (Status != VersionStatus.UnderReview)
            throw new InvalidOperationException("Versionen är inte under granskning.");
        Status = VersionStatus.Approved;
    }

    internal void Reject()
    {
        if (Status != VersionStatus.UnderReview)
            throw new InvalidOperationException("Versionen är inte under granskning.");
        Status = VersionStatus.Rejected;
    }

    private void EnsureDraft()
    {
        if (Status != VersionStatus.Draft)
            throw new InvalidOperationException("Versionen kan bara redigeras i utkastläge.");
    }
}
