using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionAggregate = ConventionSystem.Domain.Convention.Aggregates.Convention;
using EditionAggregate = ConventionSystem.Domain.Convention.Aggregates.Edition;

namespace ConventionSystem.Application.Common.Authorization;

public static class ApplicationAuthorization
{
    public static void EnsureConventionAdmin(ConventionAggregate convention, PersonId performedById, string message)
    {
        if (!convention.IsAdministrator(performedById))
            throw new ForbiddenException(message);
    }

    public static void EnsureConventionAdminOrOwner(
        ConventionAggregate convention,
        PersonId ownerId,
        PersonId performedById,
        string message)
    {
        if (!convention.IsAdministrator(performedById) && ownerId != performedById)
            throw new ForbiddenException(message);
    }

    public static void EnsureReceptionAccess(
        ConventionAggregate convention,
        EditionAggregate edition,
        PersonId performedById,
        string message)
    {
        if (!convention.IsAdministrator(performedById) && !edition.IsReceptionStaff(performedById))
            throw new ForbiddenException(message);
    }

}
