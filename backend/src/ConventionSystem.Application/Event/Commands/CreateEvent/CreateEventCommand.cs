using MediatR;

namespace ConventionSystem.Application.Event.Commands.CreateEvent;

public sealed record CreateEventCommand(
    Guid EditionId,
    Guid CategoryId,
    Guid LeadOrganiserId,
    Guid ConventionId) : IRequest<Guid>;
