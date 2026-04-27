using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ConventionSystem.Infrastructure.Email;

internal sealed class SmtpEmailService(IOptions<EmailOptions> options) : IDirectEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public Task SendAsync(EmailPayload payload, CancellationToken ct = default)
        => SendAsync(payload.To, payload.ToName, payload.Subject, payload.Body, ct);

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
