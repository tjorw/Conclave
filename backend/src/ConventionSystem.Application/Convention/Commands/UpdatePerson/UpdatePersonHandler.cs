using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.UpdatePerson;

public sealed class UpdatePersonHandler(
    IConventionRepository conventionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<UpdatePersonCommand>
{
    public async Task Handle(UpdatePersonCommand command, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(new PersonId(command.PersonId), ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(person.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!string.Equals(person.Email, command.Email, StringComparison.OrdinalIgnoreCase)
            && await personRepository.EmailExistsInConventionAsync(person.ConventionId, command.Email, ct))
            throw new InvalidOperationException($"E-postadressen '{command.Email}' är redan registrerad i denna konvention.");

        convention.UpdatePersonDetails(person, command.Name, command.Email, command.Phone);
        await personRepository.SaveAsync(ct);
    }
}
