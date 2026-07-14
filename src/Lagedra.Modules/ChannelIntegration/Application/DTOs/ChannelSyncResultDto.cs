namespace Lagedra.Modules.ChannelIntegration.Application.DTOs;

/// <summary>Summary returned after an on-demand content sync.</summary>
public sealed record ChannelSyncResultDto(
    int Pulled,
    int Created,
    int Updated,
    /// <summary>
    /// Hostaway only: true when a unified webhook was created or already present;
    /// false when registration was attempted and failed; null when not applicable
    /// (other providers, localhost, or auto-register disabled).
    /// </summary>
    bool? WebhookRegistered = null);
