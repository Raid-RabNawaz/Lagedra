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

public sealed record ListingLeaseTermsDto(
    int RentDueDayOfMonth,
    long NsfFirstFeeCents,
    long NsfSubsequentFeeCents,
    decimal LateFeePercent,
    int LateFeeGraceDays,
    string? UtilitiesResponsibility,
    bool YardMaintenanceByTenant,
    bool Furnished,
    string? IncludedAppliancesNotes,
    int KeyCount,
    int MailboxKeyCount,
    long KeyReplacementFeeCents,
    long LockoutFeeCents,
    int ParkingSpaceCount,
    string? ParkingDescription,
    bool ParkingIncludedInRent,
    int MaxGuestConsecutiveDays,
    long RentersInsuranceMinLiabilityCents,
    int EarlyTerminationFeeMonths,
    bool BuiltBefore1978,
    string? LeadPaintKnowledge,
    bool RentCapJustCauseExempt,
    string? PaymentMethods);

/// <summary>
/// Pointer to a lease agreement the host uploaded for their listing. Carries
/// the storage key because the consumer (lease generation) reads the object
/// server-side; it is never surfaced to a client.
/// </summary>
public sealed record ListingCustomLeaseDocumentDto(
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    string ContentHash,
    DateTime UploadedAtUtc);

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
    Uri? VirtualTourUrl = null,
    ListingAddressDto? PreciseAddress = null,
    ListingHouseRulesDto? HouseRules = null,
    IReadOnlyList<string>? AmenityNames = null,
    IReadOnlyList<string>? SafetyDeviceNames = null,
    IReadOnlyList<string>? ConsiderationNames = null,
    bool AcceptsPartnerDirectReservations = true,
    bool InstantBookingEnabled = false,
    long? DefaultDepositCents = null,
    // Predetermined per-verification-tier deposits. Null falls back to MaxDepositCents.
    long? DepositUnverifiedCents = null,
    long? DepositBackgroundVerifiedCents = null,
    long? DepositPartnerGuaranteedCents = null,
    ListingLeaseTermsDto? LeaseTerms = null,
    string? ManagerRole = null,
    Guid? HomeOwnerUserId = null,
    bool IncludeBrokerClause = false,
    // "LagedraTemplate" or "HostProvided" — decides whether the deal's lease is
    // generated from the jurisdiction template or is the host's own upload.
    string? LeaseAgreementSource = null,
    ListingCustomLeaseDocumentDto? CustomLeaseDocument = null);

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

    /// <summary>
    /// Phase 17 — return every listing id owned by the given landlord.
    /// Used by cross-module inboxes (e.g. the host inquiries view) that
    /// need to filter their own data by the host's listings without a
    /// per-row authorization round-trip.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetListingIdsForLandlordAsync(Guid landlordUserId, CancellationToken ct = default);

    /// <summary>
    /// Phase 17 — bulk lookup of (listing id → landlord id) used by the
    /// tenant-side "My conversations" inbox to resolve the host display
    /// name per row without expanding <see cref="ListingSummaryInfoDto"/>
    /// (and thereby breaking other consumers that already destructure it).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid>> GetLandlordIdsForListingsAsync(
        IReadOnlyList<Guid> listingIds,
        CancellationToken ct = default);
}
