using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>
/// OwnerRez channel (HA-XML) feed client. Platform credentials live in
/// <see cref="OwnerRezChannelSettings"/>; the connection only carries the host's
/// advertiser id (<c>ora…</c>). This is the read-only inquiry partnership:
/// listings, rates, and availability are pulled, and a booking is never created
/// here. Inquiry delivery is a separate step.
///
/// Not registered as <see cref="IChannelProvider"/> on its own. <see cref="OwnerRezChannelGateway"/>
/// sends connections that have no per-host token here, and sends token connections
/// to <see cref="OwnerRezChannelProvider"/>.
/// </summary>
public sealed partial class OwnerRezHaXmlChannelProvider(
    HttpClient httpClient,
    IOptions<OwnerRezChannelSettings> settings,
    ILogger<OwnerRezHaXmlChannelProvider> logger) : IChannelProvider
{
    private readonly OwnerRezChannelSettings _settings = settings.Value;

    public string ProviderKey => "ownerrez";

    public async Task<IReadOnlyList<ChannelListingSnapshot>> PullListingsAsync(
        ChannelCredentials credentials,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var advertiserId = RequireAdvertiser(credentials);

        var listingIndex = await GetRequiredAsync($"/haapi/haxml/{advertiserId}/listingindex", ct)
            .ConfigureAwait(false);
        var listings = OwnerRezHaXmlFeedParser.ReadIndex(listingIndex, "listingExternalId", "listingUrl");
        if (listings.Count == 0)
        {
            return [];
        }

        var rateIndex = await GetOptionalAsync($"/haapi/haxml/{advertiserId}/lodgingrateindex", ct)
            .ConfigureAwait(false);
        var rates = rateIndex is null
            ? []
            : OwnerRezHaXmlFeedParser.ReadIndex(rateIndex, "listingExternalId", "lodgingRateContentUrl");
        var rateById = rates.ToDictionary(link => link.ExternalId, StringComparer.OrdinalIgnoreCase);

        var snapshots = new List<ChannelListingSnapshot>();
        foreach (var listing in listings)
        {
            var listingXml = await GetOptionalAsync(ContentPath(advertiserId, "listing", listing), ct)
                .ConfigureAwait(false);
            if (listingXml is null)
            {
                LogSkipped(logger, listing.ExternalId);
                continue;
            }

            string? rateXml = null;
            if (rateById.TryGetValue(listing.ExternalId, out var rateLink))
            {
                rateXml = await GetOptionalAsync(ContentPath(advertiserId, "lodgingrate", rateLink), ct)
                    .ConfigureAwait(false);
            }

            var snapshot = OwnerRezHaXmlFeedParser.ParseListing(listingXml, rateXml);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }

    public async Task<ChannelAvailabilityCalendar> PullAvailabilityAsync(
        ChannelCredentials credentials,
        string externalListingId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var empty = new ChannelAvailabilityCalendar(externalListingId, []);
        if (!HasAdvertiser(credentials))
        {
            return empty;
        }

        var advertiserId = credentials.ExternalAccountId.Trim();
        var indexXml = await GetOptionalAsync($"/haapi/haxml/{advertiserId}/availabilityindex", ct)
            .ConfigureAwait(false);
        if (indexXml is null)
        {
            return empty;
        }

        var match = OwnerRezHaXmlFeedParser
            .ReadIndex(indexXml, "unitExternalId", "unitAvailabilityContentUrl")
            .FirstOrDefault(link => string.Equals(link.ExternalId, externalListingId, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return empty;
        }

        var availabilityXml = await GetOptionalAsync(ContentPath(advertiserId, "availability", match), ct)
            .ConfigureAwait(false);
        return availabilityXml is null
            ? empty
            : OwnerRezHaXmlFeedParser.ParseAvailability(externalListingId, availabilityXml);
    }

    public async Task<ChannelAvailabilityResult> CheckAvailabilityAsync(
        ChannelCredentials credentials,
        ChannelAvailabilityQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(query);
        if (!_settings.IsHaXmlConfigured || !HasAdvertiser(credentials))
        {
            return new ChannelAvailabilityResult(false, "NotConfigured");
        }

        if (query.CheckOut <= query.CheckIn)
        {
            return new ChannelAvailabilityResult(false, "InvalidDates");
        }

        var body = new Dictionary<string, object?>
        {
            ["requestVersion"] = "1.0",
            ["systemExternalId"] = "Lagedra",
            ["advertiserExternalId"] = credentials.ExternalAccountId.Trim(),
            ["listingExternalId"] = query.ExternalListingId,
            ["dateRange"] = new Dictionary<string, string>
            {
                ["arrivalDate"] = query.CheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["departureDate"] = query.CheckOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
            ["adults"] = Math.Max(1, query.Adults),
            ["children"] = Math.Max(0, query.Children),
            ["pets"] = Math.Max(0, query.Pets),
            ["units"] = new[]
            {
                new Dictionary<string, string> { ["unitExternalId"] = query.ExternalListingId },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/haapi/haolbjson/fastavailability")
        {
            Content = JsonContent.Create(body),
        };
        Authorize(request);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            LogHttpError(logger, nameof(CheckAvailabilityAsync), (int)response.StatusCode);
            return new ChannelAvailabilityResult(false, "RequestFailed");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("units", out var units) || units.ValueKind != JsonValueKind.Array)
        {
            return new ChannelAvailabilityResult(false, "RequestFailed");
        }

        foreach (var unit in units.EnumerateArray())
        {
            var available = unit.TryGetProperty("available", out var flag) && flag.ValueKind == JsonValueKind.True;
            if (available)
            {
                return new ChannelAvailabilityResult(true);
            }

            var errorCode = unit.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
            return new ChannelAvailabilityResult(false, string.IsNullOrWhiteSpace(errorCode) ? "Unavailable" : errorCode);
        }

        return new ChannelAvailabilityResult(false, "Unavailable");
    }

    public Task<ChannelBookingPushResult> PushBookingAsync(
        ChannelCredentials credentials,
        ChannelBookingPushRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(request);
        // The partnership sends a filled-in inquiry. It does not create a booking
        // or send card data. A successful push would record a reservation that
        // OwnerRez never received.
        return Task.FromResult(new ChannelBookingPushResult(
            false,
            ErrorCode: "InquiryOnly",
            ErrorMessage: "This OwnerRez connection imports the channel feed. Reservations stay in OwnerRez."));
    }

    public Task<IReadOnlyList<ChannelBookingUpdate>> PullBookingUpdatesAsync(
        ChannelCredentials credentials,
        DateTime changedSinceUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return Task.FromResult<IReadOnlyList<ChannelBookingUpdate>>([]);
    }

    private string RequireAdvertiser(ChannelCredentials credentials)
    {
        if (!_settings.IsHaXmlConfigured)
        {
            throw new InvalidOperationException(
                "OwnerRez channel credentials are not configured (Channels:OwnerRez:HaXmlUsername and HaXmlKey).");
        }

        if (!HasAdvertiser(credentials))
        {
            throw new InvalidOperationException("OwnerRez connection is missing a Connection ID.");
        }

        return credentials.ExternalAccountId.Trim();
    }

    private static bool HasAdvertiser(ChannelCredentials credentials) =>
        !string.IsNullOrWhiteSpace(credentials.ExternalAccountId);

    private static string ContentPath(string advertiserId, string resource, OwnerRezHaXmlContentLink link) =>
        string.IsNullOrWhiteSpace(link.ContentLocation)
            ? $"/haapi/haxml/{advertiserId}/{resource}/{link.ExternalId}"
            : link.ContentLocation;

    private async Task<string> GetRequiredAsync(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, path, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            LogHttpError(logger, "GET", (int)response.StatusCode);
            throw new HttpRequestException(Describe(response.StatusCode));
        }

        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    private async Task<string?> GetOptionalAsync(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, path, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            LogHttpError(logger, "GET", (int)response.StatusCode);
            throw new HttpRequestException(Describe(response.StatusCode));
        }

        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        Authorize(request);
        return await httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    private void Authorize(HttpRequestMessage request)
    {
        var token = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_settings.HaXmlUsername}:{_settings.HaXmlKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
    }

    private static string Describe(System.Net.HttpStatusCode status) =>
        (int)status == 401
            ? "OwnerRez rejected the channel credentials."
            : $"OwnerRez channel feed returned HTTP {(int)status}.";

    [LoggerMessage(Level = LogLevel.Warning, Message = "[OwnerRez HA-XML] skipped listing {ExternalId}")]
    private static partial void LogSkipped(ILogger logger, string externalId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[OwnerRez HA-XML] {Method} got HTTP {StatusCode}")]
    private static partial void LogHttpError(ILogger logger, string method, int statusCode);
}
