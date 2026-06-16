namespace Lagedra.Modules.ChannelIntegration.Presentation.Contracts;

/// <summary>
/// Request body to connect a host's external PMS / channel account.
/// <c>Secret</c> is the provider API token; it is encrypted server-side and
/// never returned.
/// </summary>
public sealed record ConnectChannelRequest(
    string ProviderKey,
    string ExternalAccountId,
    string DisplayName,
    string? Username,
    string? Secret);
