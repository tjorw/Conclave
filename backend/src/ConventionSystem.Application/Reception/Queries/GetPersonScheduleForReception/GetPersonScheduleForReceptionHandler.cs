using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Reception.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Reception.Queries.GetPersonScheduleForReception;

public sealed class GetPersonScheduleForReceptionHandler(
    IEditionRepository editionRepo,
    IConventionRepository conventionRepo,
    ICurrentUser currentUser,
    IReceptionScheduleRepository scheduleRepo)
    : IQueryHandler<GetPersonScheduleForReceptionQuery, PersonScheduleDto>
{
    public async Task<PersonScheduleDto> Handle(
        GetPersonScheduleForReceptionQuery query, CancellationToken ct)
    {
        var ctx = await EditionContextLoader.LoadWithReceptionStaffAsync(
            editionRepo, conventionRepo, new EditionId(query.EditionId), ct);

        ApplicationAuthorization.EnsureReceptionAccess(
            ctx.Convention, ctx.Edition, currentUser.PersonId,
            "Åtkomst kräver receptionsroll eller administratör.");

        var personId = new PersonId(query.PersonId);
        var editionId = new EditionId(query.EditionId);

        var shifts = await scheduleRepo.ListShiftsAsync(personId, editionId, ct);
        var sessions = await scheduleRepo.ListOrganiserSessionsAsync(personId, editionId, ct);

        var allDays = shifts.Select(s => s.Date)
            .Concat(sessions.Select(s => s.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var dailySummary = allDays.Select(day =>
        {
            var dayShifts = shifts.Where(s => s.Date == day).ToList();
            var daySessions = sessions.Where(s => s.Date == day).ToList();
            var shiftHours = dayShifts.Sum(s => (s.End - s.Start).TotalHours);
            var sessionHours = daySessions.Sum(s => (s.End - s.Start).TotalHours);
            return new ScheduleDaySummaryDto(
                day,
                dayShifts.Count,
                Math.Round(shiftHours, 1),
                daySessions.Count,
                Math.Round(sessionHours, 1),
                Math.Round(shiftHours + sessionHours, 1));
        }).ToList();

        var totalShiftHours = Math.Round(shifts.Sum(s => (s.End - s.Start).TotalHours), 1);
        var totalSessionHours = Math.Round(sessions.Sum(s => (s.End - s.Start).TotalHours), 1);

        return new PersonScheduleDto(
            shifts,
            sessions,
            dailySummary,
            new ScheduleTotalDto(
                totalShiftHours,
                totalSessionHours,
                Math.Round(totalShiftHours + totalSessionHours, 1),
                allDays));
    }
}
