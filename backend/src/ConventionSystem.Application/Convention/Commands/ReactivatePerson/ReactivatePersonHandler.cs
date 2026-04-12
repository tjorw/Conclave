using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.ReactivatePerson;

public sealed class ReactivatePersonHandler(
    IConventionRepository conventionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<ReactivatePersonCommand>
{
    public async Task Handle(ReactivatePersonCommand command, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(new PersonId(command.PersonId), ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(person.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        convention.ReactivatePerson(person);
        await personRepository.SaveAsync(ct);
    }
}
