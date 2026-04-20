using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.OpenRegistration;

public sealed class OpenRegistrationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<OpenRegistrationCommand>
{
    protected override async Task ExecuteAsync(OpenRegistrationCommand command, CancellationToken ct)
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
                context.Edition.OpenOrganiserRegistration(performedById);
                break;
            case RegistrationType.Staff:
                context.Edition.OpenStaffRegistration(performedById);
                break;
            case RegistrationType.Visitor:
                context.Edition.OpenVisitorRegistration(performedById);
                break;
            default:
                throw new InvalidOperationException($"Okänd registreringstyp: {command.RegistrationType}.");
        }

        await editionRepository.SaveAsync(ct);
    }
}
