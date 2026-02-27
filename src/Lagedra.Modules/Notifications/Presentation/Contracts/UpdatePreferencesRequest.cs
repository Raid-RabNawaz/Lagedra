namespace Lagedra.Modules.Notifications.Presentation.Contracts;

public sealed record UpdatePreferencesRequest(
    Dictionary<string, bool> EventOptIns);
