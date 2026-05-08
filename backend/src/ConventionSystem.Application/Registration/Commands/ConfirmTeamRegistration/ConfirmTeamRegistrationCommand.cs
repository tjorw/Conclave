namespace ConventionSystem.Application.Registration.Commands.ConfirmTeamRegistration;

public sealed record ConfirmTeamRegistrationCommand(Guid TeamEventRegistrationId) : ICommand;
