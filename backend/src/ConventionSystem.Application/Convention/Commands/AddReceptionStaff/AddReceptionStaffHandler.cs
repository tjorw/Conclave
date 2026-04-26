using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.AddReceptionStaff;

public sealed class AddReceptionStaffHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : CommandHandler<AddReceptionStaffCommand>
{
    protected override async Task ExecuteAsync(AddReceptionStaffCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var context = await EditionContextLoader.LoadWithReceptionStaffAsync(editionRepository, conventionRepository, editionId, ct);

        if (!context.Convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        var person = await personRepository.GetByIdAsync(new PersonId(command.PersonId), ct)
            ?? throw new ResourceNotFoundException("Person", command.PersonId.ToString());

        if (person.ConventionId != context.Edition.ConventionId)
            throw new ForbiddenException("Personen tillhör inte denna konvention.");

        context.Edition.AddReceptionStaff(person.Id, currentUser.PersonId);
        await editionRepository.SaveAsync(ct);
    }
}
