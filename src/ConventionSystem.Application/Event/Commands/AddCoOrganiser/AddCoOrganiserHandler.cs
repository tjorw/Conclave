using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.AddCoOrganiser;

public sealed class AddCoOrganiserHandler(
    IEventRepository eventRepository,
    IPersonRepository personRepository)
    : IRequestHandler<AddCoOrganiserCommand>
{
    public async Task Handle(AddCoOrganiserCommand command, CancellationToken ct)
    {
        var personId = new PersonId(command.PersonId);
        var conventionId = new ConventionId(command.ConventionId);

        var ev = await eventRepository.GetByIdWithCoOrganisersAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");
        if (person.ConventionId != conventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");

        ev.AddCoOrganiser(personId);
        await eventRepository.SaveAsync(ct);
    }
}
