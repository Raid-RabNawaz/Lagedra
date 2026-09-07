namespace Lagedra.Modules.Notifications.Application.DTOs;

public sealed record NotificationPreferencesDto(
    Guid UserId,
    Dictionary<string, bool> EventOptIns,
    bool TransactionalAlwaysSent,
    bool SmsCampaignsOptedIn = false,
    string? SmsPhoneE164 = null);
