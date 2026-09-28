namespace Lagedra.Modules.ChannelIntegration.Application.DTOs;

/// <summary>Minimal projection of an available channel provider for the connect UI.</summary>
/// <param name="ProviderKey">Registry key used when connecting.</param>
/// <param name="UsesOAuth">
/// True when the host links this provider by authorizing Lagedra at the provider
/// instead of pasting credentials, so the UI renders a redirect button rather than
/// a credential form. Depends on deployment config, not just the provider: OwnerRez
/// only reports true once an OAuth app's client id and secret are configured.
/// </param>
/// <param name="ConnectMode">
/// How the Channels page should collect credentials. <c>haxml</c> is the OwnerRez
/// channel Connection ID. <c>oauth</c> and <c>token</c> are the API-for-apps paths,
/// offered only when <c>Channels:OwnerRez:OpenApiEnabled</c> is true. Every other
/// provider uses <c>credentials</c>.
/// </param>
public sealed record ChannelProviderDto(string ProviderKey, bool UsesOAuth, string ConnectMode);

public static class ChannelConnectMode
{
    public const string Credentials = "credentials";
    public const string OAuth = "oauth";
    public const string Token = "token";
    public const string HaXml = "haxml";
}
