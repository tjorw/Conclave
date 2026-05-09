namespace ConventionSystem.Application.Registration.Commands.AllocateSessionRegistrations;

public sealed record AllocateSessionRegistrationsCommand(
    Guid EventId,
    Guid SessionId,
    string Strategy) : ICommand;
