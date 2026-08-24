using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lagedra.SharedKernel.Sms;
using Microsoft.Extensions.Configuration;
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
    IConfiguration configuration,
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

        // Last line of defense: whatever a caller hands us (older accounts
        // still hold numbers like "(818) 305-6520"), Twilio must only ever
        // see E.164.
        if (!PhoneNumberE164.TryNormalize(message.ToE164, out var to))
        {
            throw new InvalidOperationException(
                $"Recipient phone number '{message.ToE164}' cannot be normalized to E.164.");
        }

        var form = new Dictionary<string, string>
        {
            ["To"] = to,
            ["MessagingServiceSid"] = _settings.MessagingServiceSid,
            ["Body"] = message.Body
        };

        // Ask Twilio to report async delivery outcomes (delivered /
        // undelivered / failed) to our webhook. Without this, carrier-side
        // blocks (e.g. error 30034, unregistered A2P 10DLC sender) are
        // invisible: the API accepts the message and we log "SMS sent".
        var statusCallback = BuildStatusCallbackUrl();
        if (statusCallback is not null)
        {
            form["StatusCallback"] = statusCallback;
        }

        using var content = new FormUrlEncodedContent(form);

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

            LogSmsSent(logger, to, sid);
            return sid;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSmsFailed(logger, ex, to);
            throw;
        }
    }

    /// <summary>
    /// Public URL Twilio posts delivery-status callbacks to. Skipped when the
    /// API base URL is not configured or not publicly reachable (local dev).
    /// </summary>
    private string? BuildStatusCallbackUrl()
    {
        var baseUrl = configuration["App:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)
            || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || !Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
        {
            return null;
        }

        return $"{baseUrl.TrimEnd('/')}{TwilioWebhookPaths.SmsStatus}";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS sent to {Recipient} | Sid: {MessageSid}")]
    private static partial void LogSmsSent(ILogger logger, string recipient, string? messageSid);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send SMS to {Recipient}")]
    private static partial void LogSmsFailed(ILogger logger, Exception exception, string recipient);
}
