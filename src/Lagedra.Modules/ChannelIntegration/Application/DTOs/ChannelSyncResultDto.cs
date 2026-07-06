namespace Lagedra.Modules.ChannelIntegration.Application.DTOs;

/// <summary>Summary returned after an on-demand content sync.</summary>
public sealed record ChannelSyncResultDto(
    int Pulled,
    int Created,
    int Updated);
