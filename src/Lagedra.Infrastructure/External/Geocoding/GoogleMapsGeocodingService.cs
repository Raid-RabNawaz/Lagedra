using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Geocoding;

public sealed partial class GoogleMapsGeocodingService(
    HttpClient httpClient,
    IOptions<GoogleMapsSettings> settings,
    ILogger<GoogleMapsGeocodingService> logger)
    : IGeocodingService
{
    private readonly GoogleMapsSettings _settings = settings.Value;

    public async Task<GeocodingResult?> GeocodeAddressAsync(string address, CancellationToken ct = default)
    {
        var uri = new Uri($"{_settings.BaseUrl}/geocode/json?address={Uri.EscapeDataString(address)}&key={_settings.ApiKey}");
        var response = await httpClient.GetAsync(uri, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<GoogleGeocodeResponse>(json);

        if (result?.Status != "OK" || result.Results.Count == 0)
        {
            LogGeocodeFailed(logger, address, result?.Status ?? "null");
            return null;
        }

        var loc = result.Results[0].Geometry.Location;
        return new GeocodingResult(loc.Lat, loc.Lng, result.Results[0].FormattedAddress);
    }

    public async Task<AddressResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var uri = new Uri($"{_settings.BaseUrl}/geocode/json?latlng={latitude},{longitude}&key={_settings.ApiKey}");
        var response = await httpClient.GetAsync(uri, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<GoogleGeocodeResponse>(json);

        if (result?.Status != "OK" || result.Results.Count == 0)
        {
            return null;
        }

        var top = result.Results[0];
        return new AddressResult(
            top.FormattedAddress,
            GetComponent(top, "locality"),
            GetComponent(top, "administrative_area_level_1"),
            GetComponent(top, "country"),
            GetComponent(top, "postal_code"));
    }

    public async Task<JurisdictionResult?> ResolveJurisdictionAsync(string preciseAddress, CancellationToken ct = default)
    {
        var geocoded = await GeocodeAddressAsync(preciseAddress, ct).ConfigureAwait(false);
        if (geocoded is null)
        {
            return null;
        }

        var address = await ReverseGeocodeAsync(geocoded.Latitude, geocoded.Longitude, ct).ConfigureAwait(false);
        if (address is null)
        {
            return null;
        }

        var code = $"{address.Country?.ToUpperInvariant()}-{address.State?.ToUpperInvariant()}-{address.City?.ToUpperInvariant()}";
        return new JurisdictionResult(code, address.City ?? string.Empty, address.State ?? string.Empty, address.Country ?? string.Empty);
    }

    private static string? GetComponent(GoogleGeocodeResult result, string type) =>
        result.AddressComponents
            .FirstOrDefault(c => c.Types.Contains(type))
            ?.LongName;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Geocoding failed for address '{Address}': {Status}")]
    private static partial void LogGeocodeFailed(ILogger logger, string address, string status);

#pragma warning disable CA1812 // instantiated by System.Text.Json via reflection
    private sealed class GoogleGeocodeResponse
    {
        [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
        [JsonPropertyName("results")] public List<GoogleGeocodeResult> Results { get; init; } = [];
    }

    private sealed class GoogleGeocodeResult
    {
        [JsonPropertyName("formatted_address")] public string FormattedAddress { get; init; } = string.Empty;
        [JsonPropertyName("geometry")] public GoogleGeometry Geometry { get; init; } = new();
        [JsonPropertyName("address_components")] public List<GoogleAddressComponent> AddressComponents { get; init; } = [];
    }

    private sealed class GoogleGeometry
    {
        [JsonPropertyName("location")] public GoogleLocation Location { get; init; } = new();
    }

    private sealed class GoogleLocation
    {
        [JsonPropertyName("lat")] public double Lat { get; init; }
        [JsonPropertyName("lng")] public double Lng { get; init; }
    }

    private sealed class GoogleAddressComponent
    {
        [JsonPropertyName("long_name")] public string LongName { get; init; } = string.Empty;
        [JsonPropertyName("types")] public List<string> Types { get; init; } = [];
    }
#pragma warning restore CA1812
}
