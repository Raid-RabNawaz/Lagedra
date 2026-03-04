namespace Lagedra.Infrastructure.External.Geocoding;

public sealed record GeocodingResult(double Latitude, double Longitude, string FormattedAddress);
public sealed record AddressResult(string FormattedAddress, string? City, string? State, string? Country, string? PostalCode);
public sealed record JurisdictionResult(string JurisdictionCode, string City, string State, string Country);

public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeAddressAsync(string address, CancellationToken ct = default);
    Task<AddressResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);
    Task<JurisdictionResult?> ResolveJurisdictionAsync(string preciseAddress, CancellationToken ct = default);
}
