namespace Lagedra.Infrastructure.External.Channels.Hostaway;

/// <summary>Outcome of ensuring a Hostaway unified webhook points at Lagedra.</summary>
public enum HostawayWebhookEnsureResult
{
    Skipped,
    Created,
    AlreadyPresent,
    Failed,
}
