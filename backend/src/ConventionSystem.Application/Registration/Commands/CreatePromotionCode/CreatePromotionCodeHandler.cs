using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Commands.CreatePromotionCode;

public sealed class CreatePromotionCodeHandler(
    IPromotionCodeRepository promotionCodeRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : ICommandHandler<CreatePromotionCodeCommand, Guid>
{
    public async Task<Guid> Handle(CreatePromotionCodeCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var performedById = currentUser.PersonId;
        var normalizedCode = PromotionCode.NormalizeCode(command.Code);

        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplagan", command.EditionId.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konventionen", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(performedById))
            throw new ForbiddenException("Utföraren har inte behörighet att skapa kampanjkoder.");

        if (await promotionCodeRepository.ExistsByEditionAndCodeAsync(editionId, normalizedCode, ct))
            throw new PromotionCodeAlreadyExistsException();

        var promotionCode = new PromotionCode(
            PromotionCodeId.New(),
            editionId,
            normalizedCode,
            command.Description,
            command.DiscountType,
            command.DiscountValue,
            command.MaxRedemptions,
            command.ValidFrom,
            command.ValidUntil,
            command.AllowedTicketTypeIds,
            performedById);

        await promotionCodeRepository.AddAndSaveAsync(promotionCode, ct);
        return promotionCode.Id.Value;
    }
}
