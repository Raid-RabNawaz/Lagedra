namespace Lagedra.Modules.ChannelIntegration.Domain.Enums;

public enum ChannelConnectionStatus
{
    /// <summary>Created but not yet validated / turned on for syncing.</summary>
    PendingActivation,

    /// <summary>Actively syncing content/availability and eligible for booking push.</summary>
    Active,

    /// <summary>Turned off by the host or an admin; no syncing.</summary>
    Disabled,

    /// <summary>Last sync failed; needs attention before syncing resumes.</summary>
    Error
}
