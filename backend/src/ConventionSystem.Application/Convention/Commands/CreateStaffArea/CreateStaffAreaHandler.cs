using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.CreateStaffArea;

public sealed class CreateStaffAreaHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : ICommandHandler<CreateStaffAreaCommand, Guid>
{
    public async Task<Guid> Handle(CreateStaffAreaCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;
        var responsibleId = new PersonId(command.ResponsibleId);

        var context = await EditionContextLoader.LoadWithStaffAreasAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention,
            performedById,
            "Utföraren är inte administratör för denna konvention.");

        var responsible = await personRepository.GetByIdAsync(responsibleId, ct)
            ?? throw new InvalidOperationException($"Ansvarig person '{command.ResponsibleId}' hittades inte.");
        if (responsible.ConventionId != context.Edition.ConventionId)
            throw new InvalidOperationException("Ansvarig person tillhör inte denna konvention.");

        var staffArea = context.Edition.CreateStaffArea(command.Name, responsibleId, command.Description);
        await editionRepository.SaveAsync(ct);

        return staffArea.Id.Value;
    }
}
