using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>
/// OwnerRez implementation of <see cref="IChannelProvider"/>.
///
/// Stub for now — the real HAXML/HAOLB content feeds, the availability/quote
/// endpoints and the OLB merchant-of-record booking push are wired here once
/// sandbox credentials are issued. Every method currently logs and returns a
/// neutral result so the sync jobs and the booking publisher are safe to run
/// before the integration is live (mirrors the InsuranceApiClient stub).
/// </summary>
public sealed partial class OwnerRezChannelProvider(
    IOptions<OwnerRezChannelSettings> settings,
    ILogger<OwnerRezChannelProvider> logger) : IChannelProvider
{
    private readonly OwnerRezChannelSettings _settings = settings.Value;

    public string ProviderKey => "ownerrez";

    public Task<IReadOnlyList<ChannelListingSnapshot>> PullListingsAsync(
        ChannelCredentials credentials,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _ = ct;
        LogStub(logger, nameof(PullListingsAsync), _settings.BaseUrl);
        return Task.FromResult<IReadOnlyList<ChannelListingSnapshot>>([]);
    }

    public Task<ChannelAvailabilityCalendar> PullAvailabilityAsync(
        ChannelCredentials credentials,
        string externalListingId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _ = ct;
        LogStub(logger, nameof(PullAvailabilityAsync), _settings.BaseUrl);
        return Task.FromResult(new ChannelAvailabilityCalendar(externalListingId, []));
    }

    public Task<ChannelAvailabilityResult> CheckAvailabilityAsync(
        ChannelCredentials credentials,
        ChannelAvailabilityQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _ = ct;
        LogStub(logger, nameof(CheckAvailabilityAsync), _settings.BaseUrl);
        return Task.FromResult(new ChannelAvailabilityResult(Available: true));
    }

    public Task<ChannelBookingPushResult> PushBookingAsync(
        ChannelCredentials credentials,
        ChannelBookingPushRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(request);
        _ = ct;
        LogStub(logger, nameof(PushBookingAsync), _settings.BaseUrl);
        return Task.FromResult(new ChannelBookingPushResult(
            Success: false,
            ErrorCode: "NotImplemented",
            ErrorMessage: "OwnerRez booking push is not yet wired."));
    }

    public Task<IReadOnlyList<ChannelBookingUpdate>> PullBookingUpdatesAsync(
        ChannelCredentials credentials,
        DateTime changedSinceUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _ = ct;
        _ = changedSinceUtc;
        LogStub(logger, nameof(PullBookingUpdatesAsync), _settings.BaseUrl);
        return Task.FromResult<IReadOnlyList<ChannelBookingUpdate>>([]);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "[STUB] OwnerRezChannelProvider.{Method} called (base {BaseUrl}) — real OwnerRez integration not yet wired")]
    private static partial void LogStub(ILogger logger, string method, Uri baseUrl);
}
