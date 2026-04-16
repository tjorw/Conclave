using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyOrganiserSessions;

public sealed record GetMyOrganiserSessionsQuery(Guid EditionId) : IRequest<IReadOnlyList<MyOrganiserSessionSummaryDto>>;
