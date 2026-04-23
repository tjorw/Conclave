using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Staff.Queries.GetStaffSchedule;

public sealed record GetStaffScheduleQuery(Guid EditionId, Guid? StaffAreaId = null) : IQuery<StaffScheduleDto>;
