namespace Lagedra.SharedKernel.Sms;

public sealed class SmsMessage
{
    /// <summary>Destination phone number in E.164 format (e.g. +14155552671).</summary>
    public required string ToE164 { get; init; }

    public required string Body { get; init; }
}
