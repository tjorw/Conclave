using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.RedeemPromotionCode;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class RedeemPromotionCodeHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly IVisitorRegistrationRepository _visitorRegistrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly IPromotionCodeRepository _promotionRepo = Substitute.For<IPromotionCodeRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RedeemPromotionCodeHandler _handler;

    public RedeemPromotionCodeHandlerTests()
    {
        _handler = new RedeemPromotionCodeHandler(
            _ticketRepo,
            _ticketTypeRepo,
            _visitorRegistrationRepo,
            _promotionRepo,
            _currentUser);
    }

    [Fact]
    public async Task Handle_FreePromotion_AutoPaysTicket()
    {
        var personId = PersonId.New();
        var editionId = EditionId.New();
        var ticketTypeId = TicketTypeId.New();
        var ticket = new Ticket(TicketId.New(), ticketTypeId, personId, editionId);
        var ticketType = new TicketType(ticketTypeId, editionId, "Helgbiljett", 15000, TicketTypeCategory.Visitor);
        var promotion = new PromotionCode(
            PromotionCodeId.New(),
            editionId,
            "FREE",
            "Fri biljett",
            PromotionDiscountType.Free,
            0,
            null,
            null,
            null,
            null,
            personId);

        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>()).Returns(ticketType);
        _promotionRepo.GetByEditionAndCodeAsync(editionId, "FREE", Arg.Any<CancellationToken>()).Returns(promotion);
        _currentUser.PersonId.Returns(personId);

        var result = await _handler.Handle(new RedeemPromotionCodeCommand(ticket.Id.Value, "free"), default);

        Assert.Equal(TicketStatus.Paid, ticket.Status);
        Assert.Equal(0, result.FinalPrice);
        Assert.Equal(15000, result.DiscountApplied);
        Assert.Equal("Paid", result.TicketStatus);
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FixedDiscount_UpdatesFinalPrice()
    {
        var personId = PersonId.New();
        var editionId = EditionId.New();
        var ticketTypeId = TicketTypeId.New();
        var ticket = new Ticket(TicketId.New(), ticketTypeId, personId, editionId);
        var ticketType = new TicketType(ticketTypeId, editionId, "Helgbiljett", 15000, TicketTypeCategory.Visitor);
        var promotion = new PromotionCode(
            PromotionCodeId.New(),
            editionId,
            "SAVE100",
            "100 kr rabatt",
            PromotionDiscountType.Fixed,
            10000,
            null,
            null,
            null,
            null,
            personId);

        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>()).Returns(ticketType);
        _promotionRepo.GetByEditionAndCodeAsync(editionId, "SAVE100", Arg.Any<CancellationToken>()).Returns(promotion);
        _currentUser.PersonId.Returns(personId);

        var result = await _handler.Handle(new RedeemPromotionCodeCommand(ticket.Id.Value, "save100"), default);

        Assert.Equal(TicketStatus.Reserved, ticket.Status);
        Assert.Equal(5000, result.FinalPrice);
        Assert.Equal(10000, result.DiscountApplied);
    }

    [Fact]
    public async Task Handle_TicketBelongsToAnotherPerson_ThrowsForbidden()
    {
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), PersonId.New(), EditionId.New());
        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new RedeemPromotionCodeCommand(ticket.Id.Value, "CODE"), default));
    }
}
