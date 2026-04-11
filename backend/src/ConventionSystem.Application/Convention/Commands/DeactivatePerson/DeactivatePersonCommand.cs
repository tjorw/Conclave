using MediatR;

namespace ConventionSystem.Application.Convention.Commands.DeactivatePerson;

public sealed record DeactivatePersonCommand(Guid PersonId) : IRequest;
