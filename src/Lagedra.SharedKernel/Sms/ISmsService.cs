namespace Lagedra.SharedKernel.Sms;

public interface ISmsService
{
    /// <summary>
    /// Sends an SMS. Returns the provider message id when available (e.g. Twilio SID).
    /// </summary>
    Task<string?> SendAsync(SmsMessage message, CancellationToken ct = default);
}
