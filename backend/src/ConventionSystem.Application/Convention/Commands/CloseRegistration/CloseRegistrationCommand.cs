using ConventionSystem.Domain.Convention.Enums;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CloseRegistration;

public sealed record CloseRegistrationCommand(
    Guid EditionId,
    RegistrationType RegistrationType) : IRequest;
