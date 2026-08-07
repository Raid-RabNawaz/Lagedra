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
    Error,

    /// <summary>
    /// Disconnected by the host: credentials are wiped and the connection is
    /// hidden from the host, but the row (and its listing mappings) is retained
    /// so reconnecting the same account re-links the already-imported listings
    /// instead of importing them all over again as duplicates.
    /// </summary>
    Revoked
}
