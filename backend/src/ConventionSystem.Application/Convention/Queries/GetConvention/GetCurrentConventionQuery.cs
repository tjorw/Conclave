using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Queries.GetConvention;

public sealed record GetCurrentConventionQuery : IQuery<ConventionDto?>;
