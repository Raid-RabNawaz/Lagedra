namespace Lagedra.Modules.ListingAndLocation.Presentation.Contracts;

public sealed record LockPreciseAddressRequest(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    string? JurisdictionCode);
