using Lagedra.SharedKernel.Sms;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.External.Sms;

/// <summary>
/// Logs SMS payloads when Twilio credentials are not configured (local/dev).
/// </summary>
public sealed partial class NoOpSmsService(ILogger<NoOpSmsService> logger) : ISmsService
{
    public Task<string?> SendAsync(SmsMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        LogSkipped(logger, message.ToE164, message.Body.Length);
        return Task.FromResult<string?>(null);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "SMS skipped (Twilio not configured) to {Recipient} | BodyLength: {BodyLength}")]
    private static partial void LogSkipped(ILogger logger, string recipient, int bodyLength);
}
