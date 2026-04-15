using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.RemoveAdministrator;

public sealed class RemoveAdministratorHandler(
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : IRequestHandler<RemoveAdministratorCommand>
{
    public async Task Handle(RemoveAdministratorCommand command, CancellationToken ct)
    {
        var conventionId = new ConventionId(command.ConventionId);

        var convention = await conventionRepository.GetByIdAsync(conventionId, ct)
            ?? throw new InvalidOperationException($"Konvention '{command.ConventionId}' hittades inte.");

        var performedById = currentUser.PersonId;
        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        var person = await personRepository.GetByIdAsync(new PersonId(command.PersonId), ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");

        if (person.ConventionId != conventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");

        convention.RemoveAdministrator(person.Id, performedById);
        await conventionRepository.SaveAsync(ct);
    }
}
