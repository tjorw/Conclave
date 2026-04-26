using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListVisitorTicketTypesForWalkup;

public sealed class ListVisitorTicketTypesForWalkupHandler(
    IEditionRepository editionRepo,
    IConventionRepository conventionRepo,
    ICurrentUser currentUser,
    ITicketTypeRepository ticketTypeRepo)
    : IQueryHandler<ListVisitorTicketTypesForWalkupQuery, IReadOnlyList<VisitorTicketTypeDto>>
{
    public async Task<IReadOnlyList<VisitorTicketTypeDto>> Handle(
        ListVisitorTicketTypesForWalkupQuery query, CancellationToken ct)
    {
        var ctx = await EditionContextLoader.LoadWithReceptionStaffAsync(
            editionRepo, conventionRepo, new EditionId(query.EditionId), ct);

        ApplicationAuthorization.EnsureReceptionAccess(
            ctx.Convention, ctx.Edition, currentUser.PersonId,
            "Åtkomst kräver receptionsroll eller administratör.");

        var all = await ticketTypeRepo.ListByEditionIdAsync(ctx.Edition.Id, ct);

        return all
            .Where(t => t.Category == "Visitor")
            .Select(t => new VisitorTicketTypeDto(t.Id, t.Name, t.Price, t.Description))
            .ToList();
    }
}
