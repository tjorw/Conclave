using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Convention.Queries.SearchPersonsForReception;

public sealed class SearchPersonsForReceptionHandler(
    IEditionRepository editionRepo,
    IConventionRepository conventionRepo,
    ICurrentUser currentUser,
    IPersonRepository personRepo,
    ITicketRepository ticketRepo)
    : IQueryHandler<SearchPersonsForReceptionQuery, IReadOnlyList<PersonSearchResultDto>>
{
    public async Task<IReadOnlyList<PersonSearchResultDto>> Handle(
        SearchPersonsForReceptionQuery query, CancellationToken ct)
    {
        var ctx = await EditionContextLoader.LoadWithReceptionStaffAsync(
            editionRepo, conventionRepo, new EditionId(query.EditionId), ct);

        ApplicationAuthorization.EnsureReceptionAccess(
            ctx.Convention, ctx.Edition, currentUser.PersonId,
            "Åtkomst kräver receptionsroll eller administratör.");

        var term = query.SearchTerm.Trim();

        if (Guid.TryParse(term, out var ticketGuid))
        {
            var result = await personRepo.FindByTicketIdForReceptionAsync(ctx.Edition.Id, new TicketId(ticketGuid), ct);
            return result != null ? [result] : [];
        }

        return await personRepo.SearchForReceptionAsync(
            ctx.Convention.Id, ctx.Edition.Id, term, 20, ct);
    }
}
