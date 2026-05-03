using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Domain.Content.Events;

public sealed record PagePublished(PageId PageId, string Slug, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record PageUnpublished(PageId PageId, string Slug, DateTimeOffset OccurredAt) : IDomainEvent;
