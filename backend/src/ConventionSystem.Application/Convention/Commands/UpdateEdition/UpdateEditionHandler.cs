using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Application.Convention.Commands.UpdateEdition;

public sealed class UpdateEditionHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<UpdateEditionCommand>
{
    protected override async Task ExecuteAsync(UpdateEditionCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var context = await EditionContextLoader.LoadAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention,
            performedById,
            "Utforaren ar inte administrator for denna konvention.");

        context.Edition.UpdateDetails(
            command.Name,
            new DatePeriod(command.StartDate, command.EndDate),
            new PersonId(command.StaffCoordinatorId),
            new PersonId(command.EventCoordinatorId),
            command.ScheduleDays?
                .Select(d => new EditionScheduleDay(Guid.NewGuid(), d.Date, d.StartTime, d.EndTime))
                .ToList()
            ?? []);

        await editionRepository.SaveAsync(ct);
    }
}
