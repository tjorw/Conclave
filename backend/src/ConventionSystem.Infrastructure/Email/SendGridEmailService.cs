using ConventionSystem.Application.Common;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ConventionSystem.Infrastructure.Email;

public sealed class SendGridEmailService(IOptions<EmailOptions> options) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public Task SendVisitorRegistrationConfirmedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.VisitorRegistrationConfirmed();
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendStaffApplicationReceivedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.StaffApplicationReceived();
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendStaffApplicationAcceptedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.StaffApplicationAccepted();
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendStaffApplicationRejectedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.StaffApplicationRejected();
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendEventApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.EventApproved(eventTitle);
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendEventRejectedAsync(string toEmail, string toName, string eventTitle, string comment, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.EventRejected(eventTitle, comment);
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.PasswordReset(resetLink);
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.EmailConfirmation(confirmLink);
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendResendConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.ResendConfirmation(confirmLink);
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    public Task SendPasswordChangedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplates.PasswordChanged();
        return SendAsync(toEmail, toName, subject, body, ct);
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
        var (subject, body) = EmailTemplates.TenantSignupWelcome(
            organizationName,
            subdomain,
            temporaryPassword,
            confirmLink);
        return SendAsync(toEmail, toName, subject, body, ct);
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
        var (subject, body) = EmailTemplates.TenantProvisionedWelcome(
            organizationName,
            subdomain,
            toEmail,
            temporaryPassword,
            loginLink);
        return SendAsync(toEmail, toName, subject, body, ct);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken ct)
    {
        ValidateConfiguration();

        var from = new EmailAddress(_options.FromEmail, _options.FromName);
        var to = new EmailAddress(toEmail, toName);

        var message = MailHelper.CreateSingleEmail(from, to, subject, body, body);
        var client = new SendGridClient(_options.SendGrid.ApiKey);
        var response = await client.SendEmailAsync(message, ct);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Body.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"SendGrid kunde inte skicka e-post. Status: {(int)response.StatusCode}. Svar: {responseBody}");
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("E-postavsandare saknas i konfigurationen (Email:FromEmail).");

        if (string.IsNullOrWhiteSpace(_options.SendGrid.ApiKey))
            throw new InvalidOperationException("SendGrid API-nyckel saknas i konfigurationen (Email:SendGrid:ApiKey).");
    }
}
