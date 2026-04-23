using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;

namespace ConventionSystem.Application.Staff.Commands.UpdateShift;

public sealed class UpdateShiftHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    IStaffApplicationRepository staffApplicationRepository,
    ICurrentUser currentUser)
    : CommandHandler<UpdateShiftCommand>
{
    protected override async Task ExecuteAsync(UpdateShiftCommand command, CancellationToken ct)
    {
        var shiftId = new Domain.Staff.Ids.ShiftId(command.ShiftId);
        var targetStationId = new StationId(command.StationId);
        var responsibleId = new PersonId(command.ResponsibleId);
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
            "Utföraren har inte behörighet att uppdatera detta pass.");

        if (!context.Edition.Stations.Any(station => station.Id == targetStationId))
            throw new InvalidOperationException("Stationen hittades inte på denna upplaga.");

        ApplicationAuthorization.EnsureShiftManager(
            context.Convention,
            context.Edition,
            targetStationId,
            performedById,
            "Utföraren har inte behörighet att flytta passet till denna station.");

        var responsible = await personRepository.GetByIdAsync(responsibleId, ct)
            ?? throw new InvalidOperationException($"Skiftansvarig '{command.ResponsibleId}' hittades inte.");
        if (responsible.ConventionId != context.Edition.ConventionId)
            throw new InvalidOperationException("Skiftansvarig tillhör inte denna konvention.");
        if (!await staffApplicationRepository.HasApprovedApplicationAsync(responsibleId, context.Edition.Id, ct))
            throw new InvalidOperationException("Skiftansvarig är inte funktionär för denna upplaga.");

        var timeSlot = new TimeSlot(command.StartTime, command.EndTime);
        var staffingRequirement = new StaffingRequirement(command.MinPersons, command.MaxPersons);

        context.Shift.Update(targetStationId, responsibleId, timeSlot, staffingRequirement);
        await shiftRepository.SaveAsync(ct);
    }
}
