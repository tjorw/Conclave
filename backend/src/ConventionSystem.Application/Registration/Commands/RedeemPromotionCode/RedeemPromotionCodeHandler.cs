using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RedeemPromotionCode;

public sealed class RedeemPromotionCodeHandler(
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository,
    IVisitorRegistrationRepository visitorRegistrationRepository,
    IPromotionCodeRepository promotionCodeRepository,
    ICurrentUser currentUser)
    : IRequestHandler<RedeemPromotionCodeCommand, RedeemPromotionCodeResult>
{
    public async Task<RedeemPromotionCodeResult> Handle(RedeemPromotionCodeCommand command, CancellationToken ct)
    {
        var ticketId = new TicketId(command.TicketId);
        var personId = currentUser.PersonId;

        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", command.TicketId.ToString());

        if (ticket.PersonId != personId)
            throw new ForbiddenException("Biljetten tillhör inte denna person.");

        if (ticket.Status != TicketStatus.Reserved)
            throw new TicketNotReservedForPromotionException();

        var ticketType = await ticketTypeRepository.GetByIdAsync(ticket.TicketTypeId, ct)
            ?? throw new ResourceNotFoundException("Biljetttyp", ticket.TicketTypeId.Value.ToString());

        var normalizedCode = Domain.Registration.Aggregates.PromotionCode.NormalizeCode(command.Code);

        var promotionCode = await promotionCodeRepository.GetByEditionAndCodeAsync(ticket.EditionId, normalizedCode, ct)
            ?? throw new ResourceNotFoundException("Kampanjkod", normalizedCode);

        var redemption = promotionCode.Redeem(
            ticket.Id,
            personId,
            ticket.TicketTypeId,
            ticketType.Price,
            DateTimeOffset.UtcNow);

        ticket.ApplyPromotion(redemption.Id, redemption.FinalPrice);

        if (ticket.Status == TicketStatus.Paid)
        {
            var visitorRegistration = await visitorRegistrationRepository.GetByTicketIdAsync(ticket.Id, ct);
            if (visitorRegistration is not null && visitorRegistration.Status == VisitorRegistrationStatus.PendingPayment)
                visitorRegistration.ConfirmPayment($"PROMO:{promotionCode.Code}");
        }

        await ticketRepository.SaveAsync(ct);

        return new RedeemPromotionCodeResult(
            ticket.Id.Value,
            promotionCode.Id.Value,
            redemption.DiscountApplied,
            redemption.FinalPrice,
            ticket.Status.ToString());
    }
}
