namespace ConventionSystem.Application.Registration.Commands.RegisterTeamForEvent;

public sealed record RegisterTeamForEventCommand(
    Guid EventId,
    Guid EditionId,
    string TeamName) : IRequest<Guid>;
