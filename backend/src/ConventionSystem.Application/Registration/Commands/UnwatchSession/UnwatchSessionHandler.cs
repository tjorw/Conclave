using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.UnwatchSession;

public sealed class UnwatchSessionHandler(
    ISessionWatchRepository sessionWatchRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UnwatchSessionCommand>
{
    public Task Handle(UnwatchSessionCommand command, CancellationToken ct)
        => sessionWatchRepository.RemoveByPersonAndSessionAsync(
            currentUser.PersonId,
            new SessionId(command.SessionId),
            ct);
}
