namespace ConventionSystem.Application.Event.Commands.ConfigureTeamRegistration;

public sealed record ConfigureTeamRegistrationCommand(
    Guid EventId,
    string RegistrationMode,
    int? MinTeamSize,
    int? MaxTeamSize) : ICommand;
