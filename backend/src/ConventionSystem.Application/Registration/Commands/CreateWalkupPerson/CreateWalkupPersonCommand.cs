namespace ConventionSystem.Application.Registration.Commands.CreateWalkupPerson;

public sealed record CreateWalkupPersonCommand(
    Guid EditionId,
    string Name,
    string Email,
    string? Phone) : ICommand<Guid>;
