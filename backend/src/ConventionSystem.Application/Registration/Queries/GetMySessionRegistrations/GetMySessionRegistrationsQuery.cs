using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMySessionRegistrations;

public sealed record GetMySessionRegistrationsQuery(Guid EditionId) : IRequest<IReadOnlyList<MySessionRegistrationSummaryDto>>;
