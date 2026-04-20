using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
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

        var context = await ShiftContextLoader.LoadAsync(
            shiftRepository,
            editionRepository,
            conventionRepository,
            shiftId,
            ct);

        ApplicationAuthorization.EnsureShiftManager(
            context.Convention,
            context.Edition,
            context.Shift.StationId,
            performedById,
            "Utföraren har inte behörighet att ställa in detta pass.");

        context.Shift.Cancel(performedById);
        await shiftRepository.SaveAsync(ct);
    }
}
