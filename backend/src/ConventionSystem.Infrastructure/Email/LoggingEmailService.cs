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
        logger.LogInformation("[E-POST] Staffansökan mottagen → {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffApplicationAcceptedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Staffansökan accepterad → {Name} <{Email}>", toName, toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffApplicationRejectedAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        logger.LogInformation("[E-POST] Staffansökan avslagen → {Name} <{Email}>", toName, toEmail);
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
}
