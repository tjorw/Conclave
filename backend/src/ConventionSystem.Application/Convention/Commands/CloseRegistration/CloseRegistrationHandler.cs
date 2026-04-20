using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.CloseRegistration;

public sealed class CloseRegistrationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<CloseRegistrationCommand>
{
    protected override async Task ExecuteAsync(CloseRegistrationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var context = await EditionContextLoader.LoadAsync(
            editionRepository,
            conventionRepository,
            editionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(
            context.Convention,
            performedById,
            "Utföraren är inte administratör för denna konvention.");

        switch (command.RegistrationType)
        {
            case RegistrationType.Organiser:
                context.Edition.CloseOrganiserRegistration(performedById);
                break;
            case RegistrationType.Staff:
                context.Edition.CloseStaffRegistration(performedById);
                break;
            case RegistrationType.Visitor:
                context.Edition.CloseVisitorRegistration(performedById);
                break;
            default:
                throw new InvalidOperationException($"Stängning stöds inte för registreringstypen: {command.RegistrationType}.");
        }

        await editionRepository.SaveAsync(ct);
    }
}
