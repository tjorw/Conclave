using ConventionSystem.Domain.Convention.Enums;

namespace ConventionSystem.Application.Convention.Commands.OpenRegistration;

public sealed record OpenRegistrationCommand(
    Guid EditionId,
    RegistrationType RegistrationType) : ICommand;
