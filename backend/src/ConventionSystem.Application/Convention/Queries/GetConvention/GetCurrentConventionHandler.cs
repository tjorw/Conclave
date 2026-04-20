using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;

namespace ConventionSystem.Application.Convention.Queries.GetConvention;

public sealed class GetCurrentConventionHandler(IConventionRepository conventionRepository)
    : IQueryHandler<GetCurrentConventionQuery, ConventionDto?>
{
    public Task<ConventionDto?> Handle(GetCurrentConventionQuery query, CancellationToken ct)
        => conventionRepository.GetProjectedAsync(ct);
}
