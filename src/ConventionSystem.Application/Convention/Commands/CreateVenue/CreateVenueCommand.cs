using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreateVenue;

public sealed record CreateVenueCommand(
    Guid EditionId,
    string Name,
    string Building,
    string? Description) : IRequest<Guid>;
