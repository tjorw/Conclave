using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Application.Staff.Commands.DeleteShift;

public sealed class DeleteShiftHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<DeleteShiftCommand>
{
    protected override async Task ExecuteAsync(DeleteShiftCommand command, CancellationToken ct)
    {
        var shiftId = new ShiftId(command.ShiftId);
        var performedById = currentUser.PersonId;

        var context = await ShiftContextLoader.LoadAsync(
            shiftRepository,
            editionRepository,
            conventionRepository,
            shiftId,
            ct);
        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan ta bort pass.");
        await shiftRepository.DeleteAndSaveAsync(context.Shift, ct);
    }
}
