using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;

public sealed record GetMyVisitorRegistrationQuery(Guid EditionId) : IQuery<IReadOnlyList<MyVisitorRegistrationDto>>;
