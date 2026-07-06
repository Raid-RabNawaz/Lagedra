using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Services;

/// <summary>
/// Materialises an externally-sourced listing (e.g. an OwnerRez property pulled
/// through the channel feeds) into a Lagedra <see cref="Listing"/>. Imported
/// listings are always created as <see cref="ListingStatus.Draft"/> so the host
/// reviews pricing/details and submits for admin review — the importer never
/// auto-publishes. The operation is idempotent: callers that already mapped the
/// external id to a Lagedra listing pass that id and the importer updates it in
/// place instead of creating a duplicate.
/// </summary>
public sealed partial class ListingImporter(
    ListingsDbContext dbContext,
    IClock clock,
    ILogger<ListingImporter> logger) : IListingImporter
{
    // Lagedra requires a positive monthly rent on create. OwnerRez properties
    // are nightly and may not expose a rate through the feed; when we cannot
    // derive one we seed a placeholder so the draft can be created, and the
    // host corrects it before submitting for review.
    private const long FallbackRentCents = 1_500_00;

    public async Task<ListingImportResult> ImportOrUpdateAsync(
        ListingImportRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var monthlyRentCents = request.MonthlyRentCents > 0 ? request.MonthlyRentCents : FallbackRentCents;
        var maxDepositCents = request.MaxDepositCents > 0 ? request.MaxDepositCents : monthlyRentCents;
        var minDays = Math.Clamp(request.MinStayDays <= 0 ? 30 : request.MinStayDays, 30, 180);
        var requestedMax = request.MaxStayDays < minDays ? 180 : request.MaxStayDays;
        var maxDays = Math.Clamp(requestedMax, minDays, 180);
        var stayRange = new StayRange(minDays, maxDays);
        var bathrooms = request.Bathrooms < 0.5m ? 1m : request.Bathrooms;
        var bedrooms = Math.Max(0, request.Bedrooms);
        var propertyType = MapPropertyType(request.PropertyType);
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"Imported listing {request.ExternalListingId}"
            : request.Title.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description) ? title : request.Description.Trim();

        Listing? listing = null;
        if (request.ExistingListingId is { } existingId)
        {
            listing = await dbContext.Listings
                .Include(l => l.Photos)
                .FirstOrDefaultAsync(l => l.Id == existingId, ct)
                .ConfigureAwait(false);
        }

        var created = false;
        if (listing is null)
        {
            listing = Listing.Create(
                request.LandlordUserId,
                propertyType,
                title,
                description,
                monthlyRentCents,
                bedrooms,
                bathrooms,
                stayRange,
                maxDepositCents,
                request.SquareFootage);

            dbContext.Listings.Add(listing);
            dbContext.ListingPriceHistory.Add(ListingPriceHistory.Create(
                listing.Id, monthlyRentCents, DateOnly.FromDateTime(clock.UtcNow)));
            created = true;
        }
        else if (listing.Status is ListingStatus.Draft or ListingStatus.Denied)
        {
            // Refresh editable drafts with the latest channel content. Published
            // listings are left untouched (we only re-affirm the mapping).
            listing.Update(
                propertyType,
                title,
                description,
                monthlyRentCents,
                bedrooms,
                bathrooms,
                stayRange,
                maxDepositCents,
                request.SquareFootage);
        }

        var editable = listing.Status is ListingStatus.Draft or ListingStatus.Denied;

        if (editable && request is { Latitude: { } lat, Longitude: { } lng })
        {
            TrySetApproxLocation(listing, lat, lng);
        }

        if (request.Address is { } address
            && listing.Status is ListingStatus.Draft or ListingStatus.Denied or ListingStatus.Published)
        {
            TryLockAddress(listing, address);
        }

        if (created && request.Photos is { Count: > 0 } photos)
        {
            foreach (var photo in photos)
            {
                TryAddPhoto(listing, photo);
            }
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return new ListingImportResult(listing.Id, created);
    }

    private void TrySetApproxLocation(Listing listing, double latitude, double longitude)
    {
        try
        {
            listing.SetApproxLocation(new GeoPoint(latitude, longitude));
        }
        catch (ArgumentException ex)
        {
            LogSkippedField(logger, "approx-location", listing.Id, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            LogSkippedField(logger, "approx-location", listing.Id, ex.Message);
        }
    }

    private void TryLockAddress(Listing listing, ListingImportAddress address)
    {
        if (string.IsNullOrWhiteSpace(address.Street)
            || string.IsNullOrWhiteSpace(address.City)
            || string.IsNullOrWhiteSpace(address.State)
            || string.IsNullOrWhiteSpace(address.PostalCode)
            || string.IsNullOrWhiteSpace(address.Country))
        {
            return;
        }

        try
        {
            listing.LockPreciseAddress(
                new Address(address.Street, address.City, address.State, address.PostalCode, address.Country),
                jurisdictionCode: null);
        }
        catch (ArgumentException ex)
        {
            LogSkippedField(logger, "precise-address", listing.Id, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            LogSkippedField(logger, "precise-address", listing.Id, ex.Message);
        }
    }

    private void TryAddPhoto(Listing listing, ListingImportPhoto photo)
    {
        try
        {
            listing.AddPhoto($"ownerrez/{photo.ExternalId}", photo.Url, photo.Caption);
        }
        catch (ArgumentException ex)
        {
            LogSkippedField(logger, "photo", listing.Id, ex.Message);
        }
    }

    private static PropertyType MapPropertyType(ListingImportPropertyType type) => type switch
    {
        ListingImportPropertyType.Apartment => PropertyType.Apartment,
        ListingImportPropertyType.House => PropertyType.House,
        ListingImportPropertyType.Condo => PropertyType.Condo,
        ListingImportPropertyType.Townhouse => PropertyType.Townhouse,
        ListingImportPropertyType.Studio => PropertyType.Studio,
        ListingImportPropertyType.Loft => PropertyType.Loft,
        ListingImportPropertyType.Villa => PropertyType.Villa,
        ListingImportPropertyType.Cottage => PropertyType.Cottage,
        ListingImportPropertyType.Cabin => PropertyType.Cabin,
        _ => PropertyType.Other,
    };

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "[ListingImporter] skipped {Field} for listing {ListingId}: {Reason}")]
    private static partial void LogSkippedField(ILogger logger, string field, Guid listingId, string reason);
}
