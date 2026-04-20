using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.GetConvention;

public sealed class GetConventionHandler(IConventionRepository conventionRepository)
    : IQueryHandler<GetConventionQuery, ConventionDto?>
{
    public Task<ConventionDto?> Handle(GetConventionQuery query, CancellationToken ct)
        => conventionRepository.GetProjectedByIdAsync(new ConventionId(query.ConventionId), ct);
}
