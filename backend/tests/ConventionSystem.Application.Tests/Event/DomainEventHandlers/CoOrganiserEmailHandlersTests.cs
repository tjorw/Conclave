using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.DomainEventHandlers;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.DomainEventHandlers;

public class CoOrganiserApplicationSubmittedEmailHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly CoOrganiserApplicationSubmittedEmailHandler _handler;

    public CoOrganiserApplicationSubmittedEmailHandlerTests()
    {
        _handler = new CoOrganiserApplicationSubmittedEmailHandler(_eventRepo, _emailService);
    }

    private Domain.Event.Aggregates.Event CreateEvent(string title)
    {
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());
        ev.EditTitle(title);
        return ev;
    }

    [Fact]
    public async Task Handle_EventExists_SendsEmailToNominatedAddress()
    {
        var ev = CreateEvent("Mitt evenemang");
        var notification = new CoOrganiserApplicationSubmitted(
            CoOrganiserApplicationId.New(), ev.Id, "kandidat@example.com", PersonId.New(), DateTimeOffset.UtcNow);
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await _handler.Handle(notification, default);

        await _emailService.Received(1).SendCoOrganiserApplicationReceivedAsync(
            "kandidat@example.com", "Mitt evenemang", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_DoesNotSendEmail()
    {
        var eventId = EventId.New();
        _eventRepo.GetByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns((Domain.Event.Aggregates.Event?)null);
        var notification = new CoOrganiserApplicationSubmitted(
            CoOrganiserApplicationId.New(), eventId, "kandidat@example.com", PersonId.New(), DateTimeOffset.UtcNow);

        await _handler.Handle(notification, default);

        await _emailService.DidNotReceive().SendCoOrganiserApplicationReceivedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

public class CoOrganiserApplicationApprovedEmailHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly CoOrganiserApplicationApprovedEmailHandler _handler;

    public CoOrganiserApplicationApprovedEmailHandlerTests()
    {
        _handler = new CoOrganiserApplicationApprovedEmailHandler(_eventRepo, _personRepo, _emailService);
    }

    private Domain.Event.Aggregates.Event CreateEvent(string title)
    {
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());
        ev.EditTitle(title);
        return ev;
    }

    private static Domain.Convention.Entities.Person CreatePerson(string name, string email)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test", "test");
        return convention.CreatePerson(name, email);
    }

    [Fact]
    public async Task Handle_EventAndPersonExist_SendsApprovedEmail()
    {
        var ev = CreateEvent("Draksläppet");
        var personId = PersonId.New();
        var person = CreatePerson("Anna Svensson", "anna@example.com");
        var notification = new CoOrganiserApplicationApproved(
            CoOrganiserApplicationId.New(), ev.Id, personId, PersonId.New(), DateTimeOffset.UtcNow);
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _personRepo.GetByIdAsync(personId, Arg.Any<CancellationToken>()).Returns(person);

        await _handler.Handle(notification, default);

        await _emailService.Received(1).SendCoOrganiserApplicationApprovedAsync(
            person.Email, person.Name, "Draksläppet", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_DoesNotSendEmail()
    {
        var eventId = EventId.New();
        _eventRepo.GetByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns((Domain.Event.Aggregates.Event?)null);
        var notification = new CoOrganiserApplicationApproved(
            CoOrganiserApplicationId.New(), eventId, PersonId.New(), PersonId.New(), DateTimeOffset.UtcNow);

        await _handler.Handle(notification, default);

        await _emailService.DidNotReceive().SendCoOrganiserApplicationApprovedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PersonNotFound_DoesNotSendEmail()
    {
        var ev = CreateEvent("Draksläppet");
        var personId = PersonId.New();
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _personRepo.GetByIdAsync(personId, Arg.Any<CancellationToken>()).Returns((Domain.Convention.Entities.Person?)null);
        var notification = new CoOrganiserApplicationApproved(
            CoOrganiserApplicationId.New(), ev.Id, personId, PersonId.New(), DateTimeOffset.UtcNow);

        await _handler.Handle(notification, default);

        await _emailService.DidNotReceive().SendCoOrganiserApplicationApprovedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

public class CoOrganiserApplicationRejectedEmailHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly CoOrganiserApplicationRejectedEmailHandler _handler;

    public CoOrganiserApplicationRejectedEmailHandlerTests()
    {
        _handler = new CoOrganiserApplicationRejectedEmailHandler(_eventRepo, _emailService);
    }

    private (Domain.Event.Aggregates.Event ev, CoOrganiserApplicationId applicationId) SetupEventWithPendingApplication(
        string title, string email, string? name = null)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test", "test");
        var requestedBy = convention.CreatePerson("Ansökare", "requestedby@example.com");
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), EditionId.New(), CategoryId.New(), requestedBy.Id);
        ev.EditTitle(title);
        var application = ev.SubmitCoOrganiserApplication(email, name, null, requestedBy.Id);
        return (ev, application.Id);
    }

    [Fact]
    public async Task Handle_ApplicationExists_SendsRejectedEmail()
    {
        var (ev, applicationId) = SetupEventWithPendingApplication("Draksläppet", "kandidat@example.com", "Kalle");
        var notification = new CoOrganiserApplicationRejected(
            applicationId, ev.Id, PersonId.New(), "Platsen är full", DateTimeOffset.UtcNow);
        _eventRepo.GetByIdWithCoOrganisersAndApplicationsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await _handler.Handle(notification, default);

        await _emailService.Received(1).SendCoOrganiserApplicationRejectedAsync(
            "kandidat@example.com", "Kalle", "Draksläppet", "Platsen är full", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationWithNoName_UsesEmailAsName()
    {
        var (ev, applicationId) = SetupEventWithPendingApplication("Draksläppet", "kandidat@example.com", name: null);
        var notification = new CoOrganiserApplicationRejected(
            applicationId, ev.Id, PersonId.New(), null, DateTimeOffset.UtcNow);
        _eventRepo.GetByIdWithCoOrganisersAndApplicationsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await _handler.Handle(notification, default);

        await _emailService.Received(1).SendCoOrganiserApplicationRejectedAsync(
            "kandidat@example.com", "kandidat@example.com", "Draksläppet", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_DoesNotSendEmail()
    {
        var eventId = EventId.New();
        _eventRepo.GetByIdWithCoOrganisersAndApplicationsAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);
        var notification = new CoOrganiserApplicationRejected(
            CoOrganiserApplicationId.New(), eventId, PersonId.New(), null, DateTimeOffset.UtcNow);

        await _handler.Handle(notification, default);

        await _emailService.DidNotReceive().SendCoOrganiserApplicationRejectedAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
