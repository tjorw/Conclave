namespace ConventionSystem.Application.Registration.Commands.WalkupRegister;

public sealed record WalkupRegisterCommand(
    Guid EditionId,
    Guid PersonId,
    Guid TicketTypeId) : ICommand<Guid>;
