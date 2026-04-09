using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.AddAdministrator;

public sealed class AddAdministratorHandler(
    IConventionRepository conventionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<AddAdministratorCommand>
{
    public async Task Handle(AddAdministratorCommand command, CancellationToken ct)
    {
        var conventionId = new ConventionId(command.ConventionId);

        var convention = await conventionRepository.GetByIdAsync(conventionId, ct)
            ?? throw new InvalidOperationException($"Konvention '{command.ConventionId}' hittades inte.");

        var performedById = new PersonId(command.PerformedById);
        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        var person = await personRepository.GetByIdAsync(new PersonId(command.PersonId), ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");

        if (person.ConventionId != conventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");

        convention.AddAdministrator(person.Id, performedById);
        await conventionRepository.SaveAsync(ct);
    }
}
