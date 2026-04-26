using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetPersonTicketsForReception;

public sealed class GetPersonTicketsForReceptionHandler(
    IEditionRepository editionRepo,
    IConventionRepository conventionRepo,
    ICurrentUser currentUser,
    ITicketRepository ticketRepo)
    : IQueryHandler<GetPersonTicketsForReceptionQuery, IReadOnlyList<PersonTicketForReceptionDto>>
{
    public async Task<IReadOnlyList<PersonTicketForReceptionDto>> Handle(
        GetPersonTicketsForReceptionQuery query, CancellationToken ct)
    {
        var ctx = await EditionContextLoader.LoadWithReceptionStaffAsync(
            editionRepo, conventionRepo, new EditionId(query.EditionId), ct);

        ApplicationAuthorization.EnsureReceptionAccess(
            ctx.Convention, ctx.Edition, currentUser.PersonId,
            "Åtkomst kräver receptionsroll eller administratör.");

        return await ticketRepo.ListForReceptionAsync(new PersonId(query.PersonId), ctx.Edition.Id, ct);
    }
}
