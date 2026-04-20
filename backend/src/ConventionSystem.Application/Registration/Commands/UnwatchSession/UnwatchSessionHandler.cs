using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Registration.Commands.UnwatchSession;

public sealed class UnwatchSessionHandler(
    ISessionWatchRepository sessionWatchRepository,
    ICurrentUser currentUser)
    : CommandHandler<UnwatchSessionCommand>
{
    protected override async Task ExecuteAsync(UnwatchSessionCommand command, CancellationToken ct)
        => await sessionWatchRepository.RemoveByPersonAndSessionAsync(
            currentUser.PersonId,
            new SessionId(command.SessionId),
            ct);
}
