using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreateStaffArea;

public sealed class CreateStaffAreaHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    ICurrentUser currentUser)
    : IRequestHandler<CreateStaffAreaCommand, Guid>
{
    public async Task<Guid> Handle(CreateStaffAreaCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;
        var responsibleId = new PersonId(command.ResponsibleId);

        var edition = await editionRepository.GetByIdWithStaffAreasAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        var responsible = await personRepository.GetByIdAsync(responsibleId, ct)
            ?? throw new InvalidOperationException($"Ansvarig person '{command.ResponsibleId}' hittades inte.");
        if (responsible.ConventionId != edition.ConventionId)
            throw new InvalidOperationException("Ansvarig person tillhör inte denna konvention.");

        var staffArea = edition.CreateStaffArea(command.Name, responsibleId, command.Description);
        editionRepository.MarkAsAdded(staffArea);
        await editionRepository.SaveAsync(ct);

        return staffArea.Id.Value;
    }
}
