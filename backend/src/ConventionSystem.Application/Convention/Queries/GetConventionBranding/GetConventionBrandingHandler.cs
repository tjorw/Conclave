using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.GetConventionBranding;

public sealed class GetConventionBrandingHandler(IConventionBrandingRepository brandingRepository)
    : IQueryHandler<GetConventionBrandingQuery, ConventionBrandingDto?>
{
    public Task<ConventionBrandingDto?> Handle(GetConventionBrandingQuery query, CancellationToken ct)
        => brandingRepository.GetProjectedByConventionIdAsync(new ConventionId(query.ConventionId), ct);
}
