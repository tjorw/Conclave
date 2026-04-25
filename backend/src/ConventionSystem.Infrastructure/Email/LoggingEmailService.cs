using ConventionSystem.Application.Common;
using Microsoft.Extensions.Logging;

namespace ConventionSystem.Infrastructure.Email;

internal sealed class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService, IDirectEmailSender
{
    public Task SendVisitorRegistrationConfirmedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Visitor registration confirmed -> {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffApplicationReceivedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Staff application received -> {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffApplicationAcceptedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Staff application accepted -> {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffApplicationRejectedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Staff application rejected -> {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendEventApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Event approved -> {Name} <{Email}>, title: {Title}", toName, toEmail, eventTitle);
        return Task.CompletedTask;
    }

    public Task SendEventRejectedAsync(string toEmail, string toName, string eventTitle, string comment, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] Event rejected -> {Name} <{Email}>, title: {Title}, comment: {Comment}",
            toName,
            toEmail,
            eventTitle,
            comment);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Password reset -> {Name} <{Email}>, link: {Link}", toName, toEmail, resetLink);
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Confirm email -> {Name} <{Email}>, link: {Link}", toName, toEmail, confirmLink);
        return Task.CompletedTask;
    }

    public Task SendResendConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Resend confirmation -> {Name} <{Email}>, link: {Link}", toName, toEmail, confirmLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordChangedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Password changed -> {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
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
        logger.LogInformation(
            "[EMAIL] Tenant signup -> {Name} <{Email}>, organization: {Organization}, subdomain: {Subdomain}, temporary password: {Password}, confirmation link: {Link}",
            toName,
            toEmail,
            organizationName,
            subdomain,
            temporaryPassword,
            confirmLink);
        return Task.CompletedTask;
    }
    public Task SendCoOrganiserApplicationReceivedAsync(string toEmail, string eventTitle, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Co-organiser application received -> <{Email}>, event: {Title}", toEmail, eventTitle);
        return Task.CompletedTask;
    }

    public Task SendCoOrganiserApplicationApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Co-organiser application approved -> {Name} <{Email}>, event: {Title}", toName, toEmail, eventTitle);
        return Task.CompletedTask;
    }

    public Task SendCoOrganiserApplicationRejectedAsync(string toEmail, string toName, string eventTitle, string? comment, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Co-organiser application rejected -> {Name} <{Email}>, event: {Title}, comment: {Comment}", toName, toEmail, eventTitle, comment);
        return Task.CompletedTask;
    }

    public Task SendAsync(EmailPayload payload, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] {Subject} -> {Name} <{Email}>", payload.Subject, payload.ToName, payload.To);
        return Task.CompletedTask;
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
        logger.LogInformation(
            "[EMAIL] Tenant provisioning -> {Name} <{Email}>, organization: {Organization}, subdomain: {Subdomain}, temporary password: {Password}, login link: {Link}",
            toName,
            toEmail,
            organizationName,
            subdomain,
            temporaryPassword,
            loginLink);
        return Task.CompletedTask;
    }
}
