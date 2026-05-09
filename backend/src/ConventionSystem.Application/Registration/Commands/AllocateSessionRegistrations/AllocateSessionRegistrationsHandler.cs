using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;

namespace ConventionSystem.Application.Registration.Commands.AllocateSessionRegistrations;

public sealed class AllocateSessionRegistrationsHandler(
    ISessionRegistrationRepository sessionRegistrationRepository,
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<AllocateSessionRegistrationsCommand>
{
    protected override async Task ExecuteAsync(AllocateSessionRegistrationsCommand command, CancellationToken ct)
    {
        if (!Enum.TryParse<AllocationStrategy>(command.Strategy, out var strategy))
            throw new ArgumentException($"Ogiltig allokeringsstrategi: {command.Strategy}.");

        var eventId = new EventId(command.EventId);
        var sessionId = new SessionId(command.SessionId);

        var ev = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadAsync(editionRepository, conventionRepository, ev.EditionId, ct);
        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention, currentUser.PersonId,
            "Endast administratörer kan köra sesionstilldelning.");

        var allocationInfo = await eventRepository.GetSessionAllocationInfoAsync(sessionId, ct)
            ?? throw new ResourceNotFoundException("Session", command.SessionId.ToString());

        var pending = await sessionRegistrationRepository.GetPendingBySessionAsync(sessionId, ct);
        if (pending.Count == 0)
            return;

        var confirmedCount = await sessionRegistrationRepository.CountConfirmedBySessionIdAsync(sessionId, ct);
        var available = allocationInfo.MaxSeats - confirmedCount;

        List<SessionRegistration> toConfirm;
        List<SessionRegistration> toCancel;

        switch (strategy)
        {
            case AllocationStrategy.FirstComeFirstServed:
                toConfirm = available > 0
                    ? pending.OrderBy(r => r.CreatedAt).Take(available).ToList()
                    : [];
                toCancel = pending.Except(toConfirm).ToList();
                break;

            case AllocationStrategy.Lottery:
                toConfirm = available > 0 ? ShuffleAndTake(pending, available) : [];
                toCancel = pending.Except(toConfirm).ToList();
                break;

            case AllocationStrategy.Manual:
                toConfirm = [];
                toCancel = [];
                break;

            default:
                toConfirm = [];
                toCancel = [];
                break;
        }

        foreach (var reg in toConfirm)
            reg.Confirm();

        foreach (var reg in toCancel)
            reg.Cancel();

        var affected = toConfirm.Concat(toCancel).ToList();
        if (affected.Count > 0)
            await sessionRegistrationRepository.SaveAllAsync(affected, ct);
    }

    private static List<SessionRegistration> ShuffleAndTake(IReadOnlyList<SessionRegistration> source, int count)
    {
        var list = source.ToList();
        var rng = Random.Shared;
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list.Take(count).ToList();
    }
}
