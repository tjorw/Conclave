using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.DeactivatePerson;

public sealed class DeactivatePersonHandler(
    IConventionRepository conventionRepository,
    IPersonRepository personRepository)
    : CommandHandler<DeactivatePersonCommand>
{
    protected override async Task ExecuteAsync(DeactivatePersonCommand command, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(new PersonId(command.PersonId), ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(person.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        convention.DeactivatePerson(person);
        await personRepository.SaveAsync(ct);
    }
}
