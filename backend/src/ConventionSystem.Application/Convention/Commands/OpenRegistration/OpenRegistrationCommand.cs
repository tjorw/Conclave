using ConventionSystem.Domain.Convention.Enums;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.OpenRegistration;

public sealed record OpenRegistrationCommand(
    Guid EditionId,
    RegistrationType RegistrationType) : IRequest;
