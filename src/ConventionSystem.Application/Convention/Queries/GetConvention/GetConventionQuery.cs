using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Queries.GetConvention;

public sealed record GetConventionQuery(Guid ConventionId) : IQuery<ConventionDto?>;
