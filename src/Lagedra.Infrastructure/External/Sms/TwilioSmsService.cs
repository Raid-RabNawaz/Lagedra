using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lagedra.SharedKernel.Sms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Sms;

/// <summary>
/// Sends SMS via the Twilio Messaging API over HttpClient (no Twilio SDK —
/// the SDK's Address namespace conflicts with our domain Address type under CA1724).
/// </summary>
public sealed partial class TwilioSmsService(
    HttpClient httpClient,
    IOptions<TwilioSettings> settings,
    ILogger<TwilioSmsService> logger)
    : ISmsService
{
    private readonly TwilioSettings _settings = settings.Value;

    public async Task<string?> SendAsync(SmsMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.ToE164);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Body);

        if (!_settings.IsSmsConfigured)
        {
            throw new InvalidOperationException(
                "Twilio SMS is not configured (AccountSid, MessagingServiceSid, and AuthToken or ApiKey).");
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = message.ToE164,
            ["MessagingServiceSid"] = _settings.MessagingServiceSid,
            ["Body"] = message.Body
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"Accounts/{_settings.AccountSid}/Messages.json")
        {
            Content = content
        };

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_settings.SmsAuthUsername}:{_settings.SmsAuthPassword}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        try
        {
            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Twilio SMS failed ({(int)response.StatusCode}): {payload}");
            }

            string? sid = null;
            using (var doc = JsonDocument.Parse(payload))
            {
                if (doc.RootElement.TryGetProperty("sid", out var sidEl))
                {
                    sid = sidEl.GetString();
                }
            }

            LogSmsSent(logger, message.ToE164, sid);
            return sid;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSmsFailed(logger, ex, message.ToE164);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS sent to {Recipient} | Sid: {MessageSid}")]
    private static partial void LogSmsSent(ILogger logger, string recipient, string? messageSid);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send SMS to {Recipient}")]
    private static partial void LogSmsFailed(ILogger logger, Exception exception, string recipient);
}
