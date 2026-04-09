using MediatR;

namespace ConventionSystem.Application.Convention.Commands.AddAdministrator;

public sealed record AddAdministratorCommand(
    Guid ConventionId,
    Guid PersonId,
    Guid PerformedById) : IRequest;
