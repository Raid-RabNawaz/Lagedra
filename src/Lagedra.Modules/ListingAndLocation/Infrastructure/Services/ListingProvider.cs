using System.Globalization;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
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

        // Precise address is shared with deal participants via Truth Surface /
        // lease once the host has locked it. Public listing reads redact the
        // street separately (GetListingDetailsQuery).
        var preciseAddress = listing.PreciseAddress is not null
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

        ListingLeaseTermsDto? leaseTerms = listing.LeaseTerms is null
            ? null
            : new ListingLeaseTermsDto(
                listing.LeaseTerms.RentDueDayOfMonth,
                listing.LeaseTerms.NsfFirstFeeCents,
                listing.LeaseTerms.NsfSubsequentFeeCents,
                listing.LeaseTerms.LateFeePercent,
                listing.LeaseTerms.LateFeeGraceDays,
                listing.LeaseTerms.UtilitiesResponsibility,
                listing.LeaseTerms.YardMaintenanceByTenant,
                listing.LeaseTerms.Furnished,
                listing.LeaseTerms.IncludedAppliancesNotes,
                listing.LeaseTerms.KeyCount,
                listing.LeaseTerms.MailboxKeyCount,
                listing.LeaseTerms.KeyReplacementFeeCents,
                listing.LeaseTerms.LockoutFeeCents,
                listing.LeaseTerms.ParkingSpaceCount,
                listing.LeaseTerms.ParkingDescription,
                listing.LeaseTerms.ParkingIncludedInRent,
                listing.LeaseTerms.MaxGuestConsecutiveDays,
                listing.LeaseTerms.RentersInsuranceMinLiabilityCents,
                listing.LeaseTerms.EarlyTerminationFeeMonths,
                listing.LeaseTerms.BuiltBefore1978,
                listing.LeaseTerms.LeadPaintKnowledge,
                listing.LeaseTerms.RentCapJustCauseExempt,
                listing.LeaseTerms.PaymentMethods);

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
            listing.DefaultDepositCents,
            listing.DepositUnverifiedCents,
            listing.DepositBackgroundVerifiedCents,
            listing.DepositPartnerGuaranteedCents,
            leaseTerms,
            listing.ManagerRole.ToString(),
            listing.HomeOwnerUserId,
            listing.IncludeBrokerClause,
            listing.LeaseAgreementSource.ToString(),
            listing.CustomLeaseDocument is { } customLease
                ? new ListingCustomLeaseDocumentDto(
                    customLease.StorageKey,
                    customLease.FileName,
                    customLease.ContentType,
                    customLease.SizeBytes,
                    customLease.ContentHash,
                    customLease.UploadedAtUtc)
                : null);
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
