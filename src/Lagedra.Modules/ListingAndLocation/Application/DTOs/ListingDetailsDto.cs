using Lagedra.Modules.ListingAndLocation.Domain.Enums;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record ListingDetailsDto(
    Guid Id,
    Guid LandlordUserId,
    ListingStatus Status,
    PropertyType PropertyType,
    string Title,
    string Description,
    long MonthlyRentCents,
    bool InsuranceRequired,
    int Bedrooms,
    decimal Bathrooms,
    int? SquareFootage,
    int? MinStayDays,
    int? MaxStayDays,
    double? Latitude,
    double? Longitude,
    AddressDto? PreciseAddress,
    string? JurisdictionCode,
    long MaxDepositCents,
    long? SuggestedDepositLowCents,
    long? SuggestedDepositHighCents,
    HouseRulesDto? HouseRules,
    CancellationPolicyDto? CancellationPolicy,
    IReadOnlyList<ListingAmenityDto> Amenities,
    IReadOnlyList<ListingSafetyDeviceDto> SafetyDevices,
    IReadOnlyList<ListingConsiderationDto> Considerations,
    IReadOnlyList<ListingPhotoDto> Photos,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);
