using System.Globalization;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Services;

public sealed class ListingProvider(ListingsDbContext db) : IListingProvider
{
    public async Task<ListingDetailsDto?> GetListingDetailsAsync(Guid listingId, CancellationToken ct = default)
    {
        var listing = await db.Listings
            .AsNoTracking()
            .Include(l => l.Amenities).ThenInclude(a => a.AmenityDefinition)
            .Include(l => l.SafetyDevices).ThenInclude(s => s.SafetyDeviceDefinition)
            .Include(l => l.Considerations).ThenInclude(c => c.ConsiderationDefinition)
            .FirstOrDefaultAsync(l => l.Id == listingId, ct)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return null;
        }

        // Precise address is only embedded once the listing has been activated
        // (legally locked). Pre-activation, exposing it would leak host PII.
        var preciseAddress = listing.PreciseAddress is not null && listing.Status == ListingStatus.Activated
            ? new ListingAddressDto(
                listing.PreciseAddress.Street,
                listing.PreciseAddress.City,
                listing.PreciseAddress.State,
                listing.PreciseAddress.ZipCode,
                listing.PreciseAddress.Country)
            : null;

        var houseRules = listing.HouseRules is not null
            ? new ListingHouseRulesDto(
                listing.HouseRules.CheckInTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                listing.HouseRules.CheckOutTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                listing.HouseRules.MaxGuests,
                listing.HouseRules.PetsAllowed,
                listing.HouseRules.PetsNotes,
                listing.HouseRules.SmokingAllowed,
                listing.HouseRules.PartiesAllowed,
                listing.HouseRules.QuietHoursStart?.ToString("HH:mm", CultureInfo.InvariantCulture),
                listing.HouseRules.QuietHoursEnd?.ToString("HH:mm", CultureInfo.InvariantCulture),
                listing.HouseRules.LeavingInstructions,
                listing.HouseRules.AdditionalRules)
            : null;

        var cancellation = listing.CancellationPolicy is not null
            ? new ListingCancellationPolicyDto(
                listing.CancellationPolicy.Type,
                listing.CancellationPolicy.FreeCancellationDays,
                listing.CancellationPolicy.PartialRefundPercent,
                listing.CancellationPolicy.PartialRefundDays,
                listing.CancellationPolicy.CustomTerms)
            : null;

        // Materialise definition names. Order is deterministic so the canonical
        // hash in the Truth Surface stays stable across reads.
        var amenityNames = listing.Amenities
            .Select(a => a.AmenityDefinition?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var safetyNames = listing.SafetyDevices
            .Select(s => s.SafetyDeviceDefinition?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var considerationNames = listing.Considerations
            .Select(c => c.ConsiderationDefinition?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        return new ListingDetailsDto(
            listing.Id,
            listing.LandlordUserId,
            listing.StayRange?.MinDays,
            listing.StayRange?.MaxDays,
            listing.MaxDepositCents,
            listing.MonthlyRentCents,
            listing.JurisdictionCode,
            cancellation,
            listing.Title,
            listing.PropertyType.ToString(),
            listing.Bedrooms,
            listing.Bathrooms,
            listing.SquareFootage,
            listing.VirtualTourUrl,
            preciseAddress,
            houseRules,
            amenityNames,
            safetyNames,
            considerationNames,
            listing.AcceptsPartnerDirectReservations,
            listing.InstantBookingEnabled,
            listing.DefaultDepositCents);
    }

    public async Task<bool> IsAvailableAsync(
        Guid listingId,
        DateOnly checkIn,
        DateOnly checkOut,
        CancellationToken ct = default)
    {
        var listing = await db.Listings
            .Include(l => l.AvailabilityBlocks)
            .FirstOrDefaultAsync(l => l.Id == listingId, ct)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return false;
        }

        return AvailabilityService.IsAvailable(
            listing.AvailabilityBlocks.ToList(),
            checkIn,
            checkOut);
    }

    public async Task BlockDatesForDealAsync(
        Guid listingId,
        Guid dealId,
        DateOnly checkIn,
        DateOnly checkOut,
        CancellationToken ct = default)
    {
        var block = ListingAvailabilityBlock.CreateBooked(listingId, dealId, checkIn, checkOut);
        db.ListingAvailabilityBlocks.Add(block);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ListingSummaryInfoDto>> GetListingSummariesAsync(
        IReadOnlyList<Guid> listingIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(listingIds);

        if (listingIds.Count == 0)
        {
            return Array.Empty<ListingSummaryInfoDto>();
        }

        return await db.Listings
            .AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .Select(l => new ListingSummaryInfoDto(
                l.Id,
                l.Title,
                l.Photos.Where(p => p.IsCover).Select(p => p.Url).FirstOrDefault()
                    ?? l.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault(),
                l.PreciseAddress != null ? l.PreciseAddress.City : null))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> GetListingIdsForLandlordAsync(
        Guid landlordUserId,
        CancellationToken ct = default) =>
        await db.Listings
            .AsNoTracking()
            .Where(l => l.LandlordUserId == landlordUserId)
            .Select(l => l.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetLandlordIdsForListingsAsync(
        IReadOnlyList<Guid> listingIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(listingIds);

        if (listingIds.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var rows = await db.Listings
            .AsNoTracking()
            .Where(l => listingIds.Contains(l.Id))
            .Select(l => new { l.Id, l.LandlordUserId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ToDictionary(r => r.Id, r => r.LandlordUserId);
    }
}
