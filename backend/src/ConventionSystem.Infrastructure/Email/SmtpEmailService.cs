using ConventionSystem.Application.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ConventionSystem.Infrastructure.Email;

public sealed class SmtpEmailService(IOptions<EmailOptions> options) : IEmailService
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

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        var socketOptions = ResolveSocketOptions();
        await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, socketOptions, ct);

        if (!string.IsNullOrWhiteSpace(_options.Smtp.Username))
            await client.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }

    private SecureSocketOptions ResolveSocketOptions()
    {
        if (_options.Smtp.UseSsl)
            return SecureSocketOptions.SslOnConnect;

        if (_options.Smtp.UseStartTls)
            return SecureSocketOptions.StartTls;

        return SecureSocketOptions.None;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("E-postavsandare saknas i konfigurationen (Email:FromEmail).");

        if (string.IsNullOrWhiteSpace(_options.Smtp.Host))
            throw new InvalidOperationException("SMTP-host saknas i konfigurationen (Email:Smtp:Host).");

        if (_options.Smtp.Port <= 0)
            throw new InvalidOperationException("SMTP-port ar ogiltig i konfigurationen (Email:Smtp:Port).");

        if (!string.IsNullOrWhiteSpace(_options.Smtp.Username) && string.IsNullOrWhiteSpace(_options.Smtp.Password))
            throw new InvalidOperationException("SMTP-losenord saknas i konfigurationen (Email:Smtp:Password).");
    }
}
