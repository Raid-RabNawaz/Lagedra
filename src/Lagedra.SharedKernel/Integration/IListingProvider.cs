namespace Lagedra.SharedKernel.Integration;

public sealed record ListingCancellationPolicyDto(
    CancellationPolicyType Type,
    int FreeCancellationDays,
    int? PartialRefundPercent,
    int? PartialRefundDays,
    string? CustomTerms = null);

public sealed record ListingHouseRulesDto(
    string CheckInTime,
    string CheckOutTime,
    int MaxGuests,
    bool PetsAllowed,
    string? PetsNotes,
    bool SmokingAllowed,
    bool PartiesAllowed,
    string? QuietHoursStart,
    string? QuietHoursEnd,
    string? LeavingInstructions,
    string? AdditionalRules);

public sealed record ListingAddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);

/// <summary>
/// Detail snapshot of a published listing. Includes everything the Truth Surface
/// must embed to make the sealed deal self-describing for arbitration.
/// </summary>
public sealed record ListingDetailsDto(
    Guid Id,
    Guid LandlordUserId,
    int? MinStayDays,
    int? MaxStayDays,
    long MaxDepositCents,
    long MonthlyRentCents,
    string? JurisdictionCode,
    ListingCancellationPolicyDto? CancellationPolicy = null,
    string? Title = null,
    string? PropertyType = null,
    int Bedrooms = 0,
    decimal Bathrooms = 0m,
    int? SquareFootage = null,
    bool InsuranceRequired = false,
    Uri? VirtualTourUrl = null,
    ListingAddressDto? PreciseAddress = null,
    ListingHouseRulesDto? HouseRules = null,
    IReadOnlyList<string>? AmenityNames = null,
    IReadOnlyList<string>? SafetyDeviceNames = null,
    IReadOnlyList<string>? ConsiderationNames = null,
    bool AcceptsPartnerDirectReservations = true,
    bool InstantBookingEnabled = false);

public sealed record ListingSummaryInfoDto(
    Guid Id,
    string Title,
    Uri? CoverPhotoUri,
    string? City);

public interface IListingProvider
{
    Task<ListingDetailsDto?> GetListingDetailsAsync(Guid listingId, CancellationToken ct = default);
    Task<bool> IsAvailableAsync(Guid listingId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
    Task BlockDatesForDealAsync(Guid listingId, Guid dealId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
    Task<IReadOnlyList<ListingSummaryInfoDto>> GetListingSummariesAsync(IReadOnlyList<Guid> listingIds, CancellationToken ct = default);
}
