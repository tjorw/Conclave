using System.Text.Json;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Content;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Domain.Content.Enums;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace ConventionSystem.Infrastructure.Email;

public sealed class OutboxEmailService(
    ConventionDbContext db,
    IConfiguration configuration,
    IMailTemplateRenderer renderer,
    IMailTemplateRepository mailTemplateRepository) : IEmailService
{
    public async Task SendVisitorRegistrationConfirmedAsync(string toEmail, string toName, Guid conventionId, CancellationToken ct = default)
    {
        var (subject, body) = await ResolveTemplateAsync(
            new ConventionId(conventionId),
            MailTemplateType.VisitorRegistrationConfirmed,
            new Dictionary<string, string>
            {
                ["firstName"] = toName,
                ["conventionName"] = string.Empty,
            },
            ct);
        await EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendStaffApplicationReceivedAsync(string toEmail, string toName, Guid conventionId, CancellationToken ct = default)
    {
        var (subject, body) = await ResolveTemplateAsync(
            new ConventionId(conventionId),
            MailTemplateType.StaffApplicationReceived,
            new Dictionary<string, string>
            {
                ["firstName"] = toName,
                ["conventionName"] = string.Empty,
            },
            ct);
        await EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendStaffApplicationAcceptedAsync(string toEmail, string toName, Guid conventionId, CancellationToken ct = default)
    {
        var (subject, body) = await ResolveTemplateAsync(
            new ConventionId(conventionId),
            MailTemplateType.StaffApplicationAccepted,
            new Dictionary<string, string>
            {
                ["firstName"] = toName,
                ["conventionName"] = string.Empty,
            },
            ct);
        await EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendStaffApplicationRejectedAsync(string toEmail, string toName, Guid conventionId, CancellationToken ct = default)
    {
        var (subject, body) = await ResolveTemplateAsync(
            new ConventionId(conventionId),
            MailTemplateType.StaffApplicationRejected,
            new Dictionary<string, string>
            {
                ["firstName"] = toName,
                ["conventionName"] = string.Empty,
            },
            ct);
        await EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendEventApprovedAsync(string toEmail, string toName, string eventTitle, Guid conventionId, CancellationToken ct = default)
    {
        var (subject, body) = await ResolveTemplateAsync(
            new ConventionId(conventionId),
            MailTemplateType.EventApproved,
            new Dictionary<string, string>
            {
                ["firstName"] = toName,
                ["eventTitle"] = eventTitle,
                ["conventionName"] = string.Empty,
            },
            ct);
        await EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendEventRejectedAsync(string toEmail, string toName, string eventTitle, string comment, Guid conventionId, CancellationToken ct = default)
    {
        var (subject, body) = await ResolveTemplateAsync(
            new ConventionId(conventionId),
            MailTemplateType.EventRejected,
            new Dictionary<string, string>
            {
                ["firstName"] = toName,
                ["eventTitle"] = eventTitle,
                ["rejectionComment"] = comment,
                ["conventionName"] = string.Empty,
            },
            ct);
        await EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public async Task SendCoOrganiserInvitationAsync(string toEmail, string firstName, string eventTitle, string code, Guid conventionId, CancellationToken ct = default)
    {
        var frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:4201";
        var inviteLink = $"{frontendUrl}/accept-invitation?code={Uri.EscapeDataString(code)}";

        var (subject, body) = await ResolveTemplateAsync(
            new ConventionId(conventionId),
            MailTemplateType.CoOrganiserInvitation,
            new Dictionary<string, string>
            {
                ["firstName"] = firstName,
                ["eventTitle"] = eventTitle,
                ["inviteLink"] = inviteLink,
            },
            ct);
        await EnqueueAsync(toEmail, firstName, subject, body, ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.PasswordReset(resetLink);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.EmailConfirmation(confirmLink);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendResendConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.ResendConfirmation(confirmLink);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendPasswordChangedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.PasswordChanged();
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendTenantSignupWelcomeAsync(
        string toEmail,
        string toName,
        string organizationName,
        string subdomain,
        string temporaryPassword,
        string confirmLink,
        CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.TenantSignupWelcome(organizationName, subdomain, temporaryPassword, confirmLink);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendTenantProvisionedWelcomeAsync(
        string toEmail,
        string toName,
        string organizationName,
        string subdomain,
        string temporaryPassword,
        string loginLink,
        CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.TenantProvisionedWelcome(organizationName, subdomain, toEmail, temporaryPassword, loginLink);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    private async Task<(string Subject, string Body)> ResolveTemplateAsync(
        ConventionId conventionId,
        MailTemplateType templateType,
        Dictionary<string, string> variables,
        CancellationToken ct)
    {
        var stored = await mailTemplateRepository.GetByTypeAsync(conventionId, templateType, ct);
        var (defaultSubject, defaultBody) = DefaultMailTemplates.GetTemplate(templateType);

        var subjectTemplate = (stored?.IsCustomized == true) ? stored.Subject : defaultSubject;
        var bodyTemplate = (stored?.IsCustomized == true) ? stored.BodyMarkdown : defaultBody;

        var subject = renderer.RenderSubject(subjectTemplate, variables);
        var body = renderer.RenderBody(bodyTemplate, variables);
        return (subject, body);
    }

    private async Task EnqueueAsync(string toEmail, string toName, string subject, string body, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new EmailPayload(toEmail, toName, subject, body));
        var message = OutboxMessage.Create("EmailMessage", payload);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync(ct);
    }
}
