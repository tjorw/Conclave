using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;
using MediatR;

namespace ConventionSystem.Application.Staff.Commands.CreateShift;

public sealed class CreateShiftHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<CreateShiftCommand, Guid>
{
    public async Task<Guid> Handle(CreateShiftCommand command, CancellationToken ct)
    {
        var stationId = new StationId(command.StationId);
        var responsibleId = new PersonId(command.ResponsibleId);
        var performedById = new PersonId(command.PerformedById);

        var edition = await editionRepository.GetByStationIdAsync(stationId, ct)
            ?? throw new InvalidOperationException($"Station '{command.StationId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsibleForStation(stationId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att skapa pass för denna station.");

        var responsible = await personRepository.GetByIdAsync(responsibleId, ct)
            ?? throw new InvalidOperationException($"Skiftansvarig '{command.ResponsibleId}' hittades inte.");
        if (responsible.ConventionId != edition.ConventionId)
            throw new InvalidOperationException("Skiftansvarig tillhör inte denna konvention.");

        var timeSlot = new TimeSlot(command.StartTime, command.EndTime);
        var staffingRequirement = new StaffingRequirement(command.MinPersons, command.MaxPersons);
        var shift = new Shift(ShiftId.New(), stationId, responsibleId, timeSlot, staffingRequirement);

        await shiftRepository.AddAndSaveAsync(shift, ct);
        return shift.Id.Value;
    }
}
