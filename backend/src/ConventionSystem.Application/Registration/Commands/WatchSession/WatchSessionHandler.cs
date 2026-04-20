using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.WatchSession;

public sealed class WatchSessionHandler(
    ISessionWatchRepository sessionWatchRepository,
    ICurrentUser currentUser)
    : CommandHandler<WatchSessionCommand>
{
    protected override async Task ExecuteAsync(WatchSessionCommand command, CancellationToken ct)
    {
        var sessionId = new SessionId(command.SessionId);
        var personId = currentUser.PersonId;

        var editionId = await sessionWatchRepository.FindEditionIdBySessionIdAsync(sessionId, ct)
            ?? throw new ResourceNotFoundException("Session", command.SessionId.ToString());

        if (await sessionWatchRepository.ExistsAsync(personId, sessionId, ct))
        {
            return;
        }

        var watch = new SessionWatch(SessionWatchId.New(), personId, sessionId, editionId);
        await sessionWatchRepository.AddAndSaveAsync(watch, ct);
    }
}
