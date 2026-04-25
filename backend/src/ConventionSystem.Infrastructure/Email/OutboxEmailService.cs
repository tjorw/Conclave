using System.Text.Json;
using ConventionSystem.Application.Common;
using ConventionSystem.Infrastructure.Persistence;

namespace ConventionSystem.Infrastructure.Email;

public sealed class OutboxEmailService(ConventionDbContext db) : IEmailService
{
    public Task SendVisitorRegistrationConfirmedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.VisitorRegistrationConfirmed();
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendStaffApplicationReceivedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.StaffApplicationReceived();
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendStaffApplicationAcceptedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.StaffApplicationAccepted();
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendStaffApplicationRejectedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.StaffApplicationRejected();
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendEventApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.EventApproved(eventTitle);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendEventRejectedAsync(string toEmail, string toName, string eventTitle, string comment, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.EventRejected(eventTitle, comment);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
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

    public Task SendCoOrganiserApplicationReceivedAsync(string toEmail, string eventTitle, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.CoOrganiserApplicationReceived(eventTitle);
        return EnqueueAsync(toEmail, toEmail, subject, body, ct);
    }

    public Task SendCoOrganiserApplicationApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.CoOrganiserApplicationApproved(eventTitle);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendCoOrganiserApplicationRejectedAsync(string toEmail, string toName, string eventTitle, string? comment, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.CoOrganiserApplicationRejected(eventTitle, comment);
        return EnqueueAsync(toEmail, toName, subject, body, ct);
    }

    private async Task EnqueueAsync(string toEmail, string toName, string subject, string body, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new EmailPayload(toEmail, toName, subject, body));
        var message = OutboxMessage.Create("EmailMessage", payload);
        db.OutboxMessages.Add(message);
        await db.SaveChangesAsync(ct);
    }
}
