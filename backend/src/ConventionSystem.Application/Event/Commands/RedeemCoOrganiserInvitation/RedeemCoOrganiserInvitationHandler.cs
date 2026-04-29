using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;

namespace ConventionSystem.Application.Event.Commands.RedeemCoOrganiserInvitation;

public sealed class RedeemCoOrganiserInvitationHandler(
    IEventRepository eventRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : CommandHandler<RedeemCoOrganiserInvitationCommand>
{
    protected override async Task ExecuteAsync(RedeemCoOrganiserInvitationCommand command, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(currentUser.PersonId, ct)
            ?? throw new ResourceNotFoundException("Person", currentUser.PersonId.Value.ToString());

        var ev = await eventRepository.GetByInvitationCodeAsync(command.Code, ct)
            ?? throw new ResourceNotFoundException("Inbjudan", command.Code);

        ev.RedeemInvitation(command.Code, person.Email, currentUser.PersonId);
        await eventRepository.SaveAsync(ct);
    }
}
