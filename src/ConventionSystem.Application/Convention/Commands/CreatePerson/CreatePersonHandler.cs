using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreatePerson;

public sealed class CreatePersonHandler(
    IConventionRepository conventionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<CreatePersonCommand, Guid>
{
    public async Task<Guid> Handle(CreatePersonCommand command, CancellationToken ct)
    {
        var conventionId = new ConventionId(command.ConventionId);

        var convention = await conventionRepository.GetByIdAsync(conventionId, ct)
            ?? throw new InvalidOperationException($"Konvention '{command.ConventionId}' hittades inte.");

        if (await personRepository.EmailExistsInConventionAsync(conventionId, command.Email, ct))
            throw new InvalidOperationException($"E-postadressen '{command.Email}' är redan registrerad i denna konvention.");

        var person = convention.CreatePerson(command.Name, command.Email, command.Phone);
        await personRepository.AddAndSaveAsync(person, ct);
        return person.Id.Value;
    }
}
