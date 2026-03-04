namespace Lagedra.Infrastructure.External.Geocoding;

public sealed class GoogleMapsSettings
{
    public const string SectionName = "GoogleMaps";

    public required string ApiKey { get; init; }
    public Uri BaseUrl { get; init; } = new Uri("https://maps.googleapis.com/maps/api");
}
