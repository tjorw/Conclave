using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Queries;

namespace ConventionSystem.Application.Convention.Queries.GetConventionBranding;

public sealed record GetConventionBrandingQuery(Guid ConventionId) : IQuery<ConventionBrandingDto?>;
