using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Enums;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Convention.Commands.OpenRegistration;

public sealed class OpenRegistrationHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<OpenRegistrationCommand>
{
    public async Task Handle(OpenRegistrationCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{command.EditionId}' hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById))
            throw new InvalidOperationException("Utföraren är inte administratör för denna konvention.");

        switch (command.RegistrationType)
        {
            case RegistrationType.Organiser:
                edition.OpenOrganiserRegistration(performedById);
                break;
            case RegistrationType.Staff:
                edition.OpenStaffRegistration(performedById);
                break;
            case RegistrationType.Visitor:
                edition.OpenVisitorRegistration(performedById);
                break;
            default:
                throw new InvalidOperationException($"Okänd registreringstyp: {command.RegistrationType}.");
        }

        await editionRepository.SaveAsync(ct);
    }
}
