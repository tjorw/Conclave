using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.AddCoOrganiser;

public sealed class AddCoOrganiserHandler(
    IEventRepository eventRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : CommandHandler<AddCoOrganiserCommand>
{
    protected override async Task ExecuteAsync(AddCoOrganiserCommand command, CancellationToken ct)
    {
        var conventionId = new ConventionId(command.ConventionId);
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithCoOrganisersAndApplicationsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var leadOrganiser = await personRepository.GetByIdAsync(ev.LeadOrganiserId, ct)
            ?? throw new ResourceNotFoundException("Person", ev.LeadOrganiserId.Value.ToString());
        if (leadOrganiser.ConventionId != conventionId)
            throw new InvalidOperationException("Evenemangets huvudarrangör tillhör inte denna konvention.");

        var existingPerson = await personRepository.FindByEmailInConventionAsync(conventionId, command.Email, ct);
        if (existingPerson is not null && ev.CoOrganisers.Any(c => c.PersonId == existingPerson.Id))
            throw new CoOrganiserAlreadyAddedException();

        ev.SubmitCoOrganiserApplication(
            command.Email,
            command.Name,
            command.Message,
            performedById,
            leadOrganiser.Email);
        await eventRepository.SaveAsync(ct);
    }
}
