using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Sms;

namespace Lagedra.Modules.Notifications.Domain.Entities;

/// <summary>
/// A2P 10DLC consent for automated SMS campaigns, keyed by mobile number.
/// Transactional verification codes are sent only when the user requests them
/// and are not gated by this record.
/// </summary>
public sealed class SmsConsent : Entity<Guid>
{
    public const string SourceWebForm = "WebForm";
    public const string SourcePreferences = "Preferences";
    public const string SourceKeyword = "Keyword";

    public string PhoneE164 { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public bool OptedIn { get; private set; }
    public DateTime? OptedInAt { get; private set; }
    public DateTime? OptedOutAt { get; private set; }
    public string Source { get; private set; } = string.Empty;

    private SmsConsent() { }

    private SmsConsent(string phoneE164)
        : base(Guid.NewGuid())
    {
        PhoneE164 = phoneE164;
    }

    public static SmsConsent Create(string phoneE164)
    {
        if (!PhoneNumberE164.TryNormalize(phoneE164, out var normalized))
        {
            throw new ArgumentException("Enter a valid mobile number.", nameof(phoneE164));
        }

        return new SmsConsent(normalized);
    }

    public void OptIn(string source, DateTime utcNow, Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        OptedIn = true;
        OptedInAt = utcNow;
        OptedOutAt = null;
        Source = source.Trim();
        if (userId is { } id && id != Guid.Empty)
        {
            UserId = id;
        }
    }

    public void OptOut(string source, DateTime utcNow, Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        OptedIn = false;
        OptedOutAt = utcNow;
        Source = source.Trim();
        if (userId is { } id && id != Guid.Empty)
        {
            UserId = id;
        }
    }

    public void AttachUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        UserId = userId;
    }
}
