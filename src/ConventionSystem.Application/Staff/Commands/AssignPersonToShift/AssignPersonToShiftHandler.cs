using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Ids;
using MediatR;

namespace ConventionSystem.Application.Staff.Commands.AssignPersonToShift;

public sealed class AssignPersonToShiftHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository)
    : IRequestHandler<AssignPersonToShiftCommand, Guid>
{
    public async Task<Guid> Handle(AssignPersonToShiftCommand command, CancellationToken ct)
    {
        var shiftId = new ShiftId(command.ShiftId);
        var personId = new PersonId(command.PersonId);
        var performedById = new PersonId(command.PerformedById);

        var shift = await shiftRepository.GetByIdWithAssignmentsAsync(shiftId, ct)
            ?? throw new InvalidOperationException($"Pass '{command.ShiftId}' hittades inte.");

        var edition = await editionRepository.GetByStationIdAsync(shift.StationId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsibleForStation(shift.StationId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att tilldela personal för detta pass.");

        var person = await personRepository.GetByIdAsync(personId, ct)
            ?? throw new InvalidOperationException($"Person '{command.PersonId}' hittades inte.");
        if (person.ConventionId != edition.ConventionId)
            throw new InvalidOperationException("Personen tillhör inte denna konvention.");

        var assignment = shift.AssignPerson(personId, performedById);
        await shiftRepository.SaveAsync(ct);

        return assignment.Id.Value;
    }
}
