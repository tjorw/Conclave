using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ConventionSystem.Infrastructure.Email;

internal sealed class SendGridEmailService(IOptions<EmailOptions> options) : IDirectEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendAsync(EmailPayload payload, CancellationToken ct = default)
        => SendAsync(payload.To, payload.ToName, payload.Subject, payload.Body, ct);

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
