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
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        if (ev.LeadOrganiserId != currentUser.PersonId)
            throw new ForbiddenException("Endast huvudarrangören kan ange önskat antal medarrangörer.");

        ev.SetCoOrganiserCount(command.Count);
        await eventRepository.SaveAsync(ct);
    }
}
