using MediatR;

namespace ConventionSystem.Application.Convention.Commands.RemoveAdministrator;

public sealed record RemoveAdministratorCommand(
    Guid ConventionId,
    Guid PersonId) : IRequest;
