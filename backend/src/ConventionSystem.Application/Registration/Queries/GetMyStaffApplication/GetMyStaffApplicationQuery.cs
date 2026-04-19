using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetMyStaffApplication;

public sealed record GetMyStaffApplicationQuery(Guid EditionId) : IQuery<MyStaffApplicationDto?>;
