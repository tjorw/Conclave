using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.UpdateEdition;

public sealed class UpdateEditionHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateEditionCommand>
{
    public async Task Handle(UpdateEditionCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        edition.UpdateDetails(
            command.Name,
            new DatePeriod(command.StartDate, command.EndDate),
            new PersonId(command.StaffCoordinatorId),
            new PersonId(command.EventCoordinatorId));

        await editionRepository.SaveAsync(ct);
    }
}
