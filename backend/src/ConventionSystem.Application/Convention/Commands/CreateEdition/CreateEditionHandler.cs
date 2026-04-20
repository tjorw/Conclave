using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Application.Convention.Commands.CreateEdition;

public sealed class CreateEditionHandler(
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    IEditionRepository editionRepository,
    ICurrentUser currentUser)
    : ICommandHandler<CreateEditionCommand, Guid>
{
    public async Task<Guid> Handle(CreateEditionCommand command, CancellationToken ct)
    {
        var conventionId = new ConventionId(command.ConventionId);

        var convention = await conventionRepository.GetByIdAsync(conventionId, ct)
            ?? throw new InvalidOperationException($"Konvention '{command.ConventionId}' hittades inte.");

        var performedById = currentUser.PersonId;
        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        var staffCoordinatorId = new PersonId(command.StaffCoordinatorId);
        var staffCoordinator = await personRepository.GetByIdAsync(staffCoordinatorId, ct)
            ?? throw new InvalidOperationException($"Bemanningskoordinator '{command.StaffCoordinatorId}' hittades inte.");
        if (staffCoordinator.ConventionId != conventionId)
            throw new InvalidOperationException("Bemanningskoordinatorn tillhör inte denna konvention.");

        var eventCoordinatorId = new PersonId(command.EventCoordinatorId);
        var eventCoordinator = await personRepository.GetByIdAsync(eventCoordinatorId, ct)
            ?? throw new InvalidOperationException($"Evenemangskoordinator '{command.EventCoordinatorId}' hittades inte.");
        if (eventCoordinator.ConventionId != conventionId)
            throw new InvalidOperationException("Evenemangskoordinatorn tillhör inte denna konvention.");

        var period = new DatePeriod(command.StartDate, command.EndDate);
        var edition = convention.CreateEdition(command.Name, period, staffCoordinatorId, eventCoordinatorId);

        await editionRepository.AddAndSaveAsync(edition, ct);
        return edition.Id.Value;
    }
}
