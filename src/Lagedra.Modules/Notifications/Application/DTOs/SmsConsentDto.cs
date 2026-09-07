namespace Lagedra.Modules.Notifications.Application.DTOs;

public sealed record SmsConsentDto(
    string PhoneE164,
    bool OptedIn,
    DateTime? OptedInAt,
    DateTime? OptedOutAt);
