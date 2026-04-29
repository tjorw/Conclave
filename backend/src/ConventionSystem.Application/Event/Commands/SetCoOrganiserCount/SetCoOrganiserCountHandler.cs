using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.SetCoOrganiserCount;

public sealed class SetCoOrganiserCountHandler(
    IEventRepository eventRepository,
    ICurrentUser currentUser)
    : CommandHandler<SetCoOrganiserCountCommand>
{
    protected override async Task ExecuteAsync(SetCoOrganiserCountCommand command, CancellationToken ct)
    {
        var eventId = new EventId(command.EventId);
        var eventAggregate = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        EnsureCurrentUserIsLeadOrganiser(eventAggregate);

        eventAggregate.SetCoOrganiserCount(command.Count);
        await eventRepository.SaveAsync(ct);
    }

    private void EnsureCurrentUserIsLeadOrganiser(Domain.Event.Aggregates.Event eventAggregate)
    {
        if (eventAggregate.LeadOrganiserId != currentUser.PersonId)
            throw new ForbiddenException("Endast huvudarrangören kan ange önskat antal medarrangörer.");
    }
}
