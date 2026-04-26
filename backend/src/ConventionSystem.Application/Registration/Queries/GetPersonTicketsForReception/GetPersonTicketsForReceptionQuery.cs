using ConventionSystem.Application.Registration.Queries;

namespace ConventionSystem.Application.Registration.Queries.GetPersonTicketsForReception;

public sealed record GetPersonTicketsForReceptionQuery(Guid PersonId, Guid EditionId)
    : IQuery<IReadOnlyList<PersonTicketForReceptionDto>>;
