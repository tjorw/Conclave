using ConventionSystem.Domain.Convention.Enums;

namespace ConventionSystem.Application.Convention.Commands.CloseRegistration;

public sealed record CloseRegistrationCommand(
    Guid EditionId,
    RegistrationType RegistrationType) : ICommand;
