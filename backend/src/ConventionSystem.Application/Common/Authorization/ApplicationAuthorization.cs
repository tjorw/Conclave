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
            throw new InvalidOperationException(message);
    }

    public static void EnsureStaffApplicationManager(
        ConventionAggregate convention,
        EditionAggregate edition,
        PersonId performedById,
        string message)
    {
        if (!convention.IsAdministrator(performedById) && !edition.IsStaffCoordinator(performedById))
            throw new ForbiddenException(message);
    }

    public static void EnsureShiftManager(
        ConventionAggregate convention,
        EditionAggregate edition,
        StationId stationId,
        PersonId performedById,
        string message)
    {
        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsibleForStation(stationId, performedById))
        {
            throw new ForbiddenException(message);
        }
    }

    public static void EnsureStationManager(
        ConventionAggregate convention,
        EditionAggregate edition,
        StationId stationId,
        PersonId performedById,
        string message)
    {
        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsibleForStation(stationId, performedById))
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void EnsureStaffAreaManager(
        ConventionAggregate convention,
        EditionAggregate edition,
        StaffAreaId staffAreaId,
        PersonId performedById,
        string message)
    {
        if (!convention.IsAdministrator(performedById)
            && !edition.IsStaffCoordinator(performedById)
            && !edition.IsStaffAreaResponsible(staffAreaId, performedById))
        {
            throw new InvalidOperationException(message);
        }
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

    public static void EnsureCategoryManager(
        ConventionAggregate convention,
        EditionAggregate edition,
        CategoryId categoryId,
        PersonId performedById,
        string message)
    {
        if (!convention.IsAdministrator(performedById)
            && !edition.IsCategoryResponsible(categoryId, performedById))
        {
            throw new ForbiddenException(message);
        }
    }
}
