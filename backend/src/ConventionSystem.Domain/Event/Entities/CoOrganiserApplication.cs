using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class CoOrganiserApplication
{
    public CoOrganiserApplicationId Id { get; private set; }
    public EventId EventId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string? Name { get; private set; }
    public string? Message { get; private set; }
    public CoOrganiserApplicationStatus Status { get; private set; }
    public PersonId RequestedById { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public PersonId? ReviewedById { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewComment { get; private set; }
    public PersonId? ApprovedPersonId { get; private set; }

    private CoOrganiserApplication() { }

    internal CoOrganiserApplication(
        CoOrganiserApplicationId id,
        EventId eventId,
        string email,
        string normalizedEmail,
        string? name,
        string? message,
        PersonId requestedById)
    {
        Id = id;
        EventId = eventId;
        Email = email;
        NormalizedEmail = normalizedEmail;
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        Status = CoOrganiserApplicationStatus.Pending;
        RequestedById = requestedById;
        RequestedAt = DateTimeOffset.UtcNow;
    }

    internal void Approve(PersonId personId, PersonId reviewedById)
    {
        EnsurePending();
        Status = CoOrganiserApplicationStatus.Approved;
        ApprovedPersonId = personId;
        ReviewedById = reviewedById;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    internal void Reject(PersonId reviewedById, string? comment)
    {
        EnsurePending();
        Status = CoOrganiserApplicationStatus.Rejected;
        ReviewedById = reviewedById;
        ReviewedAt = DateTimeOffset.UtcNow;
        ReviewComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    internal void Cancel()
    {
        EnsurePending();
        Status = CoOrganiserApplicationStatus.Cancelled;
    }

    private void EnsurePending()
    {
        if (Status != CoOrganiserApplicationStatus.Pending)
            throw new CoOrganiserApplicationAlreadyReviewedException();
    }
}
