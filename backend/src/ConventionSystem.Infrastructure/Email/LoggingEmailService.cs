using ConventionSystem.Application.Common;
using Microsoft.Extensions.Logging;

namespace ConventionSystem.Infrastructure.Email;

// Platshållare tills riktig SMTP/SendGrid-implementation finns.
// Loggar e-postinnehållet på Information-nivå så att det syns under utveckling.
public sealed class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendVisitorRegistrationConfirmedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Besöksregistrering bekräftad → {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffApplicationReceivedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Funktionärsansökan mottagen → {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffApplicationAcceptedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Funktionärsansökan accepterad → {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffApplicationRejectedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Funktionärsansökan avslagen → {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendEventApprovedAsync(string toEmail, string toName, string eventTitle, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Evenemang godkänt → {Name} <{Email}>, titel: {Title}", toName, toEmail, eventTitle);
        return Task.CompletedTask;
    }

    public Task SendEventRejectedAsync(string toEmail, string toName, string eventTitle, string comment, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Evenemang avvisat → {Name} <{Email}>, titel: {Title}, kommentar: {Comment}",
            toName, toEmail, eventTitle, comment);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Lösenordsåterställning → {Name} <{Email}>, länk: {Link}", toName, toEmail, resetLink);
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Bekräfta e-post → {Name} <{Email}>, länk: {Link}", toName, toEmail, confirmLink);
        return Task.CompletedTask;
    }

    public Task SendResendConfirmationAsync(string toEmail, string toName, string confirmLink, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Ny bekräftelselänk → {Name} <{Email}>, länk: {Link}", toName, toEmail, confirmLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordChangedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Lösenord ändrat → {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }
}
