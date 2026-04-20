using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.AddCoOrganiser;

public sealed class AddCoOrganiserHandler(
    IEventRepository eventRepository,
    IPersonRepository personRepository)
    : CommandHandler<AddCoOrganiserCommand>
{
    protected override async Task ExecuteAsync(AddCoOrganiserCommand command, CancellationToken ct)
    {
        var personId = new PersonId(command.PersonId);
        var conventionId = new ConventionId(command.ConventionId);

        var ev = await eventRepository.GetByIdWithCoOrganisersAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new ResourceNotFoundException("Person", command.PersonId.ToString());
        if (person.ConventionId != conventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");

        ev.AddCoOrganiser(personId);
        await eventRepository.SaveAsync(ct);
    }
}
