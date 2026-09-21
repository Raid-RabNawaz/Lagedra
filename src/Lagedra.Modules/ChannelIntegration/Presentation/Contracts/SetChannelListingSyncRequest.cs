namespace Lagedra.Modules.ChannelIntegration.Presentation.Contracts;

public sealed record SetChannelListingSyncRequest(
    bool SyncPriceFromPms,
    bool SyncCalendarFromPms);
