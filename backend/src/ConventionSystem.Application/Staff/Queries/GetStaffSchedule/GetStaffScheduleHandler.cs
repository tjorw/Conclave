using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Staff.Queries.GetStaffSchedule;

public sealed class GetStaffScheduleHandler(
    IShiftRepository shiftRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetStaffScheduleQuery, StaffScheduleDto>
{
    public async Task<StaffScheduleDto> Handle(GetStaffScheduleQuery query, CancellationToken ct)
    {
        var editionId = new EditionId(query.EditionId);
        var context = await EditionContextLoader.LoadWithStructureAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);

        if (query.StaffAreaId is Guid staffAreaGuid)
        {
            var staffAreaId = new StaffAreaId(staffAreaGuid);
            if (!context.Edition.StaffAreas.Any(a => a.Id == staffAreaId))
                throw new InvalidOperationException($"Funktionsområde '{staffAreaGuid}' hittades inte på upplagan.");

            if (!context.Convention.IsAdministrator(currentUser.PersonId)
                && !context.Edition.IsStaffCoordinator(currentUser.PersonId)
                && !context.Edition.IsStaffAreaResponsible(staffAreaId, currentUser.PersonId))
            {
                throw new ForbiddenException("Utföraren har inte behörighet att visa detta bemanningsschema.");
            }

            return await shiftRepository.GetStaffScheduleAsync(editionId, staffAreaId, ct);
        }

        if (!context.Convention.IsAdministrator(currentUser.PersonId)
            && !context.Edition.IsStaffCoordinator(currentUser.PersonId))
        {
            throw new ForbiddenException("Utföraren har inte behörighet att visa detta bemanningsschema.");
        }

        return await shiftRepository.GetStaffScheduleAsync(editionId, null, ct);
    }
}
