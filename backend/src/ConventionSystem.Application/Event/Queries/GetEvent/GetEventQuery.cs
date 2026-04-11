using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Queries.GetEvent;

public sealed record GetEventQuery(Guid EventId) : IQuery<EventDto?>;
