using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
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
            ?? throw new ResourceNotFoundException("Konvention", command.ConventionId.ToString());

        var performedById = currentUser.PersonId;
        if (!convention.IsAdministrator(performedById))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        var person = await personRepository.GetByIdAsync(new PersonId(command.PersonId), ct)
            ?? throw new ResourceNotFoundException("Person", command.PersonId.ToString());

        if (person.ConventionId != conventionId)
            throw new ForbiddenException("Personen tillhör inte denna konvention.");

        convention.RemoveAdministrator(person.Id, performedById);
        await conventionRepository.SaveAsync(ct);
    }
}
