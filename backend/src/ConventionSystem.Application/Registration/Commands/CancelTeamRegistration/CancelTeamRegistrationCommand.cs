namespace ConventionSystem.Application.Registration.Commands.CancelTeamRegistration;

public sealed record CancelTeamRegistrationCommand(Guid TeamEventRegistrationId) : ICommand;
