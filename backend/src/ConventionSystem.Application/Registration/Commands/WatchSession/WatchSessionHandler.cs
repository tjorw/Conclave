using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.WatchSession;

public sealed class WatchSessionHandler(
    ISessionWatchRepository sessionWatchRepository,
    ICurrentUser currentUser)
    : IRequestHandler<WatchSessionCommand>
{
    public async Task Handle(WatchSessionCommand command, CancellationToken ct)
    {
        var sessionId = new SessionId(command.SessionId);
        var personId = currentUser.PersonId;

        var editionId = await sessionWatchRepository.FindEditionIdBySessionIdAsync(sessionId, ct)
            ?? throw new InvalidOperationException("Sessionen hittades inte.");

        if (await sessionWatchRepository.ExistsAsync(personId, sessionId, ct))
        {
            return;
        }

        var watch = new SessionWatch(SessionWatchId.New(), personId, sessionId, editionId);
        await sessionWatchRepository.AddAndSaveAsync(watch, ct);
    }
}
