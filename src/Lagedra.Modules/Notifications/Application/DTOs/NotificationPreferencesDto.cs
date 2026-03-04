namespace Lagedra.Modules.Notifications.Application.DTOs;

public sealed record NotificationPreferencesDto(
    Guid UserId,
    Dictionary<string, bool> EventOptIns,
    bool TransactionalAlwaysSent);
