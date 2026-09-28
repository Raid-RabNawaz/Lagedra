namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module importer that materialises an externally-sourced listing (e.g.
/// pulled from a PMS / channel such as OwnerRez) into a Lagedra listing.
/// Implemented by ListingAndLocation, consumed by ChannelIntegration.
///
/// The import is idempotent on (<see cref="ListingImportRequest.ExternalSource"/>,
/// <see cref="ListingImportRequest.ExternalListingId"/>): when a Lagedra listing
/// already exists for that pair the caller passes its id in
/// <see cref="ListingImportRequest.ExistingListingId"/> and the importer updates
/// it in place instead of creating a duplicate. Imported listings are created as
/// <c>Draft</c> so the host can review pricing/details and submit for review —
/// nothing is auto-published.
/// </summary>
public interface IListingImporter
{
    Task<ListingImportResult> ImportOrUpdateAsync(
        ListingImportRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Writes <paramref name="monthlyRentCents"/> onto an existing listing and
    /// records price history when the amount changes. Returns false when the
    /// listing is missing or not editable (in review or closed).
    /// </summary>
    Task<bool> TryApplyChannelMonthlyRentAsync(
        Guid listingId,
        long monthlyRentCents,
        CancellationToken ct = default);
}

public enum ListingImportPropertyType
{
    Apartment,
    House,
    Condo,
    Townhouse,
    Studio,
    Loft,
    Villa,
    Cottage,
    Cabin,
    Other
}

public sealed record ListingImportAddress(
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);

public sealed record ListingImportPhoto(
    string ExternalId,
    Uri Url,
    string? Caption);

public sealed record ListingImportRequest(
    Guid LandlordUserId,
    string ExternalSource,
    string ExternalListingId,
    Guid? ExistingListingId,
    string Title,
    string? Description,
    long MonthlyRentCents,
    long MaxDepositCents,
    int Bedrooms,
    decimal Bathrooms,
    int MinStayDays,
    int MaxStayDays,
    ListingImportPropertyType PropertyType,
    int? SquareFootage = null,
    double? Latitude = null,
    double? Longitude = null,
    ListingImportAddress? Address = null,
    IReadOnlyList<ListingImportPhoto>? Photos = null,
    IReadOnlyList<string>? AmenityNames = null,
    bool SyncPriceFromPms = false,
    /// <summary>
    /// True when the PMS amount was interpreted as already-monthly rent.
    /// Content sync then corrects a stored rent that is still that amount × 30 or × 31,
    /// even when <paramref name="SyncPriceFromPms"/> is false.
    /// </summary>
    bool PmsAmountIsMonthlyRent = false);

public sealed record ListingImportResult(
    Guid ListingId,
    bool Created);
