using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyStaffApplication;

public sealed record GetMyStaffApplicationQuery(Guid EditionId) : IRequest<MyStaffApplicationDto?>;
