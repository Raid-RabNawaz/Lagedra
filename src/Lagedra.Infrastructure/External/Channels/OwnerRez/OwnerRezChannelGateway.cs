namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>
/// The <c>ownerrez</c> provider the sync jobs resolve. A connection that still
/// holds an API-for-apps token (personal access token or OAuth access token) keeps
/// using <see cref="OwnerRezChannelProvider"/>. A connection with only an advertiser
/// id uses the HA-XML feed. Turning <see cref="OwnerRezChannelSettings.OpenApiEnabled"/>
/// back on restores the token and OAuth connect screens; it does not delete either client.
/// </summary>
public sealed class OwnerRezChannelGateway(
    OwnerRezChannelProvider openApi,
    OwnerRezHaXmlChannelProvider haXml) : IChannelProvider
{
    public string ProviderKey => "ownerrez";

    public static bool UsesOpenApiCredentials(ChannelCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return !string.IsNullOrWhiteSpace(credentials.Secret);
    }

    public Task<IReadOnlyList<ChannelListingSnapshot>> PullListingsAsync(
        ChannelCredentials credentials, CancellationToken ct = default) =>
        Select(credentials).PullListingsAsync(credentials, ct);

    public Task<ChannelAvailabilityCalendar> PullAvailabilityAsync(
        ChannelCredentials credentials, string externalListingId, CancellationToken ct = default) =>
        Select(credentials).PullAvailabilityAsync(credentials, externalListingId, ct);

    public Task<ChannelAvailabilityResult> CheckAvailabilityAsync(
        ChannelCredentials credentials, ChannelAvailabilityQuery query, CancellationToken ct = default) =>
        Select(credentials).CheckAvailabilityAsync(credentials, query, ct);

    public Task<ChannelBookingPushResult> PushBookingAsync(
        ChannelCredentials credentials, ChannelBookingPushRequest request, CancellationToken ct = default) =>
        Select(credentials).PushBookingAsync(credentials, request, ct);

    public Task<IReadOnlyList<ChannelBookingUpdate>> PullBookingUpdatesAsync(
        ChannelCredentials credentials, DateTime changedSinceUtc, CancellationToken ct = default) =>
        Select(credentials).PullBookingUpdatesAsync(credentials, changedSinceUtc, ct);

    private IChannelProvider Select(ChannelCredentials credentials) =>
        UsesOpenApiCredentials(credentials) ? openApi : haXml;
}
