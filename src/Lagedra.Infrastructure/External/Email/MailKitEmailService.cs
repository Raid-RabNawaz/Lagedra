using Lagedra.SharedKernel.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Lagedra.Infrastructure.External.Email;

public sealed partial class MailKitEmailService(
    IOptions<BrevoSmtpSettings> settings,
    ILogger<MailKitEmailService> logger)
    : IEmailService
{
    private readonly BrevoSmtpSettings _settings = settings.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        mime.To.Add(new MailboxAddress(message.ToName ?? message.To, message.To));
        mime.Subject = message.Subject;

        if (message.ReplyTo is not null)
        {
            mime.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
        }

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };
        mime.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, ct).ConfigureAwait(false);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct).ConfigureAwait(false);
            await client.SendAsync(mime, ct).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, ct).ConfigureAwait(false);

            LogEmailSent(logger, message.To, message.Subject);
        }
        catch (Exception ex)
        {
            LogEmailFailed(logger, ex, message.To, message.Subject);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {Recipient} | Subject: {Subject}")]
    private static partial void LogEmailSent(ILogger logger, string recipient, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email to {Recipient} | Subject: {Subject}")]
    private static partial void LogEmailFailed(ILogger logger, Exception exception, string recipient, string subject);
}
