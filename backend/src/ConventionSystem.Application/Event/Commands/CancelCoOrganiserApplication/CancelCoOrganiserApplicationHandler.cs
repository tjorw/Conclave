using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.CancelCoOrganiserApplication;

public sealed class CancelCoOrganiserApplicationHandler(
    IEventRepository eventRepository,
    ICurrentUser currentUser)
    : CommandHandler<CancelCoOrganiserApplicationCommand>
{
    protected override async Task ExecuteAsync(CancelCoOrganiserApplicationCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithCoOrganisersAndApplicationsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        ev.CancelCoOrganiserApplication(
            new CoOrganiserApplicationId(command.ApplicationId),
            currentUser.PersonId);
        await eventRepository.SaveAsync(ct);
    }
}
