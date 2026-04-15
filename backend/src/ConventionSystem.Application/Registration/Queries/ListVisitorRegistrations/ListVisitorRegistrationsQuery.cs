using MediatR;

namespace ConventionSystem.Application.Registration.Queries.ListVisitorRegistrations;

public sealed record ListVisitorRegistrationsQuery(Guid EditionId) : IRequest<IReadOnlyList<VisitorRegistrationAdminDto>>;
