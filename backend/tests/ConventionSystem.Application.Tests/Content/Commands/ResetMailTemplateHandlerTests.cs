using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Content.Commands.ResetMailTemplate;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Content.Commands;

public sealed class ResetMailTemplateHandlerTests
{
    private readonly IMailTemplateRepository _mailTemplateRepo = Substitute.For<IMailTemplateRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly Common.ICurrentUser _currentUser = Substitute.For<Common.ICurrentUser>();
    private readonly ResetMailTemplateHandler _handler;

    public ResetMailTemplateHandlerTests()
    {
        _handler = new ResetMailTemplateHandler(_mailTemplateRepo, _conventionRepo, _currentUser);
    }

    private Domain.Convention.Aggregates.Convention Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        _currentUser.PersonId.Returns(admin.Id);
        _conventionRepo.GetSingleAsync(Arg.Any<CancellationToken>()).Returns(convention);
        return convention;
    }

    [Fact]
    public async Task Handle_CustomizedTemplate_SetsIsCustomizedFalse()
    {
        var convention = Setup();
        var template = new MailTemplate(
            MailTemplateId.New(),
            convention.Id,
            MailTemplateType.EventApproved,
            "Anpassat ämne",
            "Anpassad body");
        _mailTemplateRepo.GetByTypeAsync(convention.Id, MailTemplateType.EventApproved, Arg.Any<CancellationToken>())
            .Returns(template);

        await _handler.Handle(new ResetMailTemplateCommand(convention.Id.Value, "EventApproved"), default);

        Assert.False(template.IsCustomized);
        await _mailTemplateRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomizedTemplate_RestoresDefaultText()
    {
        var convention = Setup();
        var template = new MailTemplate(
            MailTemplateId.New(),
            convention.Id,
            MailTemplateType.EventApproved,
            "Anpassat ämne",
            "Anpassad body");
        _mailTemplateRepo.GetByTypeAsync(convention.Id, MailTemplateType.EventApproved, Arg.Any<CancellationToken>())
            .Returns(template);

        await _handler.Handle(new ResetMailTemplateCommand(convention.Id.Value, "EventApproved"), default);

        var (expectedSubject, expectedBody) = Application.Content.DefaultMailTemplates.GetTemplate(MailTemplateType.EventApproved);
        Assert.Equal(expectedSubject, template.Subject);
        Assert.Equal(expectedBody, template.BodyMarkdown);
    }

    [Fact]
    public async Task Handle_NoStoredTemplate_DoesNotSave()
    {
        var convention = Setup();
        _mailTemplateRepo.GetByTypeAsync(convention.Id, MailTemplateType.EventApproved, Arg.Any<CancellationToken>())
            .Returns((MailTemplate?)null);

        await _handler.Handle(new ResetMailTemplateCommand(convention.Id.Value, "EventApproved"), default);

        await _mailTemplateRepo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownTemplateType_Throws()
    {
        Setup();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new ResetMailTemplateCommand(Guid.NewGuid(), "OkändTyp"), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var outsider = convention.RegisterPerson("Utomstående", "other@example.com");
        _currentUser.PersonId.Returns(outsider.Id);
        _conventionRepo.GetSingleAsync(Arg.Any<CancellationToken>()).Returns(convention);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new ResetMailTemplateCommand(convention.Id.Value, "EventApproved"), default));
    }
}
