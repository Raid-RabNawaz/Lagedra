using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Lagedra.Infrastructure.External.Sms;
using Lagedra.SharedKernel.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Email;

/// <summary>
/// Sends transactional email via Twilio SendGrid's v3 Mail Send API
/// (no SendGrid SDK — keeps dependencies lean, same pattern as Twilio SMS).
/// </summary>
public sealed partial class SendGridEmailService(
    HttpClient httpClient,
    IOptions<TwilioSettings> settings,
    ILogger<SendGridEmailService> logger)
    : IEmailService
{
    private readonly TwilioSettings _settings = settings.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_settings.IsEmailConfigured)
        {
            throw new InvalidOperationException(
                "Twilio SendGrid email is not configured (Twilio:SendGridApiKey and Twilio:FromEmail).");
        }

        var payload = new SendGridMailRequest
        {
            Personalizations =
            [
                new SendGridPersonalization
                {
                    To =
                    [
                        new SendGridEmailAddress
                        {
                            Email = message.To,
                            Name = message.ToName
                        }
                    ]
                }
            ],
            From = new SendGridEmailAddress
            {
                Email = _settings.FromEmail,
                Name = _settings.FromName
            },
            Subject = message.Subject,
            Content = BuildContent(message),
            ReplyTo = string.IsNullOrWhiteSpace(message.ReplyTo)
                ? null
                : new SendGridEmailAddress { Email = message.ReplyTo },
            Attachments = BuildAttachments(message.Attachments)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "mail/send")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.SendGridApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"SendGrid email failed ({(int)response.StatusCode}): {body}");
            }

            LogEmailSent(logger, message.To, message.Subject);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogEmailFailed(logger, ex, message.To, message.Subject);
            throw;
        }
    }

    private static List<SendGridContent> BuildContent(EmailMessage message)
    {
        var content = new List<SendGridContent>();
        var plain = EmailFooter.AppendPlainText(message.PlainTextBody);
        if (!string.IsNullOrWhiteSpace(plain))
        {
            content.Add(new SendGridContent { Type = "text/plain", Value = plain });
        }

        content.Add(new SendGridContent
        {
            Type = "text/html",
            Value = EmailFooter.AppendHtml(message.HtmlBody)
        });

        return content;
    }

    private static List<SendGridAttachment>? BuildAttachments(
        IReadOnlyList<EmailAttachment>? attachments)
    {
        if (attachments is not { Count: > 0 })
        {
            return null;
        }

        return attachments
            .Select(a => new SendGridAttachment
            {
                Content = Convert.ToBase64String(a.Content),
                Type = a.ContentType,
                Filename = a.FileName
            })
            .ToList();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent to {Recipient} | Subject: {Subject}")]
    private static partial void LogEmailSent(ILogger logger, string recipient, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email to {Recipient} | Subject: {Subject}")]
    private static partial void LogEmailFailed(ILogger logger, Exception exception, string recipient, string subject);

    private sealed class SendGridMailRequest
    {
        [JsonPropertyName("personalizations")]
        public required List<SendGridPersonalization> Personalizations { get; init; }

        [JsonPropertyName("from")]
        public required SendGridEmailAddress From { get; init; }

        [JsonPropertyName("subject")]
        public required string Subject { get; init; }

        [JsonPropertyName("content")]
        public required List<SendGridContent> Content { get; init; }

        [JsonPropertyName("reply_to")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SendGridEmailAddress? ReplyTo { get; init; }

        [JsonPropertyName("attachments")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<SendGridAttachment>? Attachments { get; init; }
    }

    private sealed class SendGridPersonalization
    {
        [JsonPropertyName("to")]
        public required List<SendGridEmailAddress> To { get; init; }
    }

    private sealed class SendGridEmailAddress
    {
        [JsonPropertyName("email")]
        public required string Email { get; init; }

        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; init; }
    }

    private sealed class SendGridContent
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("value")]
        public required string Value { get; init; }
    }

    private sealed class SendGridAttachment
    {
        [JsonPropertyName("content")]
        public required string Content { get; init; }

        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("filename")]
        public required string Filename { get; init; }
    }
}
