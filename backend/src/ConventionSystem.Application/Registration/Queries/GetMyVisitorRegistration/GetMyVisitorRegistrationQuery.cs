using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;

public sealed record GetMyVisitorRegistrationQuery(Guid EditionId) : IRequest<IReadOnlyList<MyVisitorRegistrationDto>>;
