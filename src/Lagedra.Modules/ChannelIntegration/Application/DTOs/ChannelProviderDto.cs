namespace Lagedra.Modules.ChannelIntegration.Application.DTOs;

/// <summary>Minimal projection of an available channel provider for the connect UI.</summary>
/// <param name="ProviderKey">Registry key used when connecting.</param>
/// <param name="UsesOAuth">
/// True when the host links this provider by authorizing Lagedra at the provider
/// instead of pasting credentials, so the UI renders a redirect button rather than
/// a credential form. Depends on deployment config, not just the provider: OwnerRez
/// only reports true once an OAuth app's client id and secret are configured.
/// </param>
public sealed record ChannelProviderDto(string ProviderKey, bool UsesOAuth);
