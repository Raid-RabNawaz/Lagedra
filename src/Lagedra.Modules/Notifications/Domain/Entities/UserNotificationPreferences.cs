using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Notifications.Domain.Entities;

public sealed class UserNotificationPreferences : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Dictionary<string, bool> EventOptIns { get; private set; } = [];
    public bool TransactionalAlwaysSent { get; private set; } = true;

    private UserNotificationPreferences() { }

    public UserNotificationPreferences(Guid userId)
        : base(Guid.NewGuid())
    {
        UserId = userId;
    }

    public void SetEventOptIn(string eventType, bool optedIn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        EventOptIns[eventType] = optedIn;
    }

    public bool IsOptedIn(string eventType)
    {
        return EventOptIns.TryGetValue(eventType, out var optedIn) && optedIn;
    }
}
