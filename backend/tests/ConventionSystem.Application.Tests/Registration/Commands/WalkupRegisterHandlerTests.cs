using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.WalkupRegister;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class WalkupRegisterHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly IVisitorRegistrationRepository _visitorRegistrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly WalkupRegisterHandler _handler;

    public WalkupRegisterHandlerTests()
    {
        _handler = new WalkupRegisterHandler(
            _editionRepo, _conventionRepo, _currentUser,
            _personRepo, _ticketTypeRepo, _ticketRepo, _visitorRegistrationRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Entities.Person receptionist,
             Domain.Convention.Entities.Person visitor,
             TicketType ticketType) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evtCoord = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staffCoord.Id, evtCoord.Id);
        var receptionist = convention.CreatePerson("Receptionist", "reception@example.com");
        edition.AddReceptionStaff(receptionist.Id, admin.Id);
        var visitor = convention.CreatePerson("Visitor", "visitor@example.com");

        var ticketTypeId = TicketTypeId.New();
        var ticketType = new TicketType(ticketTypeId, edition.Id, "Dagsbiljett", 150, TicketTypeCategory.Visitor);

        _editionRepo.GetByIdWithReceptionStaffAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(visitor.Id, Arg.Any<CancellationToken>()).Returns(visitor);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>()).Returns(ticketType);
        _visitorRegistrationRepo.HasActiveRegistrationForTicketTypeAsync(
            visitor.Id, edition.Id, ticketTypeId, Arg.Any<CancellationToken>()).Returns(false);

        return (convention, edition, admin, receptionist, visitor, ticketType);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsTicketAndRegistration()
    {
        var (_, edition, admin, _, visitor, ticketType) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var ticketId = await _handler.Handle(
            new WalkupRegisterCommand(edition.Id.Value, visitor.Id.Value, ticketType.Id.Value), default);

        Assert.NotEqual(Guid.Empty, ticketId);
        _ticketRepo.Received(1).Add(Arg.Any<Domain.Registration.Aggregates.Ticket>());
        await _visitorRegistrationRepo.Received(1).AddAndSaveAsync(
            Arg.Any<Domain.Registration.Aggregates.VisitorRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReceptionStaffMember_HasAccess()
    {
        var (_, edition, _, receptionist, visitor, ticketType) = Setup();
        _currentUser.PersonId.Returns(receptionist.Id);

        var ticketId = await _handler.Handle(
            new WalkupRegisterCommand(edition.Id.Value, visitor.Id.Value, ticketType.Id.Value), default);

        Assert.NotEqual(Guid.Empty, ticketId);
    }

    [Fact]
    public async Task Handle_NonReceptionNonAdmin_ThrowsForbiddenException()
    {
        var (convention, edition, _, _, visitor, ticketType) = Setup();
        var outsider = convention.CreatePerson("Outsider", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new WalkupRegisterCommand(edition.Id.Value, visitor.Id.Value, ticketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_PersonFromOtherConvention_ThrowsForbiddenException()
    {
        var (_, edition, admin, _, _, ticketType) = Setup();
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other", "other");
        var foreignPerson = otherConvention.CreatePerson("Foreign", "foreign@example.com");
        _personRepo.GetByIdAsync(foreignPerson.Id, Arg.Any<CancellationToken>()).Returns(foreignPerson);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new WalkupRegisterCommand(edition.Id.Value, foreignPerson.Id.Value, ticketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_DuplicateRegistration_ThrowsDomainRuleViolation()
    {
        var (_, edition, admin, _, visitor, ticketType) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        _visitorRegistrationRepo.HasActiveRegistrationForTicketTypeAsync(
            visitor.Id, edition.Id, ticketType.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<Domain.Common.DomainRuleViolationException>(
            () => _handler.Handle(
                new WalkupRegisterCommand(edition.Id.Value, visitor.Id.Value, ticketType.Id.Value), default));
    }
}
