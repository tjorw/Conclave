namespace ConventionSystem.Application.Common;

public interface IEmailService
{
    Task SendVisitorRegistrationConfirmedAsync(string toEmail, string toName, CancellationToken ct = default);
    Task SendStaffApplicationReceivedAsync(string toEmail, string toName, CancellationToken ct = default);
    Task SendStaffApplicationAcceptedAsync(string toEmail, string toName, CancellationToken ct = default);
    Task SendStaffApplicationRejectedAsync(string toEmail, string toName, CancellationToken ct = default);
    Task SendEventApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default);
    Task SendEventRejectedAsync(string toEmail, string toName, string eventTitle, string comment, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default);
    Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default);
    Task SendResendConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default);
    Task SendPasswordChangedAsync(string toEmail, string toName, CancellationToken ct = default);
    Task SendTenantSignupWelcomeAsync(
        string toEmail,
        string toName,
        string organizationName,
        string subdomain,
        string temporaryPassword,
        string confirmLink,
        CancellationToken ct = default);
}
