
namespace ConventionSystem.Application.Registration.Commands.CancelSessionRegistration;

public sealed record CancelSessionRegistrationCommand(
    Guid SessionRegistrationId) : ICommand;
