using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

internal static class ListingMapper
{
    public static ListingDetailsDto ToDetails(Listing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);
        
        return new ListingDetailsDto(
            listing.Id,
            listing.LandlordUserId,
            listing.Status,
            listing.PropertyType,
            listing.Title,
            listing.Description,
            listing.MonthlyRentCents,
            listing.InsuranceRequired,
            listing.Bedrooms,
            listing.Bathrooms,
            listing.SquareFootage,
            listing.StayRange?.MinDays,
            listing.StayRange?.MaxDays,
            listing.ApproxGeoPoint?.Latitude,
            listing.ApproxGeoPoint?.Longitude,
            listing.PreciseAddress is { } addr
                ? new AddressDto(addr.Street, addr.City, addr.State, addr.ZipCode, addr.Country)
                : null,
            listing.JurisdictionCode,
            listing.MaxDepositCents,
            listing.SuggestedDepositLowCents,
            listing.SuggestedDepositHighCents,
            listing.HouseRules is { } hr
                ? new HouseRulesDto(
                    hr.CheckInTime, hr.CheckOutTime, hr.MaxGuests,
                    hr.PetsAllowed, hr.PetsNotes, hr.SmokingAllowed,
                    hr.PartiesAllowed, hr.QuietHoursStart, hr.QuietHoursEnd,
                    hr.LeavingInstructions, hr.AdditionalRules)
                : null,
            listing.CancellationPolicy is { } cp
                ? new CancellationPolicyDto(
                    cp.Type, cp.FreeCancellationDays,
                    cp.PartialRefundPercent, cp.PartialRefundDays,
                    cp.CustomTerms)
                : null,
            listing.Amenities
                .Where(a => a.AmenityDefinition is not null)
                .Select(a => new ListingAmenityDto(
                    a.AmenityDefinitionId,
                    a.AmenityDefinition!.Name,
                    a.AmenityDefinition.Category,
                    a.AmenityDefinition.IconKey))
                .ToList(),
            listing.SafetyDevices
                .Where(s => s.SafetyDeviceDefinition is not null)
                .Select(s => new ListingSafetyDeviceDto(
                    s.SafetyDeviceDefinitionId,
                    s.SafetyDeviceDefinition!.Name,
                    s.SafetyDeviceDefinition.IconKey))
                .ToList(),
            listing.Considerations
                .Where(c => c.ConsiderationDefinition is not null)
                .Select(c => new ListingConsiderationDto(
                    c.ConsiderationDefinitionId,
                    c.ConsiderationDefinition!.Name,
                    c.ConsiderationDefinition.IconKey))
                .ToList(),
            listing.Photos
                .OrderBy(p => p.SortOrder)
                .Select(p => new ListingPhotoDto(
                    p.Id, p.Url, p.Caption, p.IsCover, p.SortOrder))
                .ToList(),
            listing.CreatedAt,
            listing.UpdatedAt);
    }

    public static ListingSummaryDto ToSummary(Listing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        return new ListingSummaryDto(
            listing.Id,
            listing.Title,
            listing.Status,
            listing.PropertyType,
            listing.MonthlyRentCents,
            listing.InsuranceRequired,
            listing.Bedrooms,
            listing.Bathrooms,
            listing.StayRange?.MinDays,
            listing.StayRange?.MaxDays,
            listing.ApproxGeoPoint?.Latitude,
            listing.ApproxGeoPoint?.Longitude,
            listing.Photos.FirstOrDefault(p => p.IsCover)?.Url
                ?? listing.Photos.OrderBy(p => p.SortOrder).FirstOrDefault()?.Url,
            listing.CreatedAt);
    }
}
