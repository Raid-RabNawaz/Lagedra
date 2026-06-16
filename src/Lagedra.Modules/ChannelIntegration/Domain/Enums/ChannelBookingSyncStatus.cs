namespace Lagedra.Modules.ChannelIntegration.Domain.Enums;

public enum ChannelBookingSyncStatus
{
    /// <summary>Queued for push to the channel; not yet delivered.</summary>
    Pending,

    /// <summary>Successfully recorded on the channel (external id captured).</summary>
    Pushed,

    /// <summary>Push attempt failed; retryable by a reconciliation job.</summary>
    Failed,

    /// <summary>The channel reported the booking cancelled on its side.</summary>
    CancelledRemotely
}
