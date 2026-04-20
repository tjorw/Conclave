using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Application.Staff.Commands.CancelShift;

public sealed class CancelShiftHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<CancelShiftCommand>
{
    protected override async Task ExecuteAsync(CancelShiftCommand command, CancellationToken ct)
    {
        var shiftId = new ShiftId(command.ShiftId);
        var performedById = currentUser.PersonId;

        var shift = await shiftRepository.GetByIdAsync(shiftId, ct)
            ?? throw new InvalidOperationException($"Pass '{command.ShiftId}' hittades inte.");

        var edition = await editionRepository.GetByStationIdAsync(shift.StationId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsibleForStation(shift.StationId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att ställa in detta pass.");

        shift.Cancel(performedById);
        await shiftRepository.SaveAsync(ct);
    }
}
