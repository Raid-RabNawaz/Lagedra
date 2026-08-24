using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ListingAndLocation.Presentation.Contracts;

public sealed record CreateListingRequest(
    PropertyType PropertyType,
    string Title,
    string Description,
    long MonthlyRentCents,
    int Bedrooms,
    decimal Bathrooms,
    int MinStayDays,
    int MaxStayDays,
    long MaxDepositCents,
    int? SquareFootage = null,
    HouseRulesRequest? HouseRules = null,
    CancellationPolicyRequest? CancellationPolicy = null,
    IReadOnlyList<Guid>? AmenityIds = null,
    IReadOnlyList<Guid>? SafetyDeviceIds = null,
    IReadOnlyList<Guid>? ConsiderationIds = null,
    bool InstantBookingEnabled = false,
    Uri? VirtualTourUrl = null,
    long? DefaultDepositCents = null,
    long? DepositUnverifiedCents = null,
    long? DepositBackgroundVerifiedCents = null,
    long? DepositPartnerGuaranteedCents = null,
    ListingManagerRole ManagerRole = ListingManagerRole.Owner,
    Guid? HomeOwnerUserId = null,
    string? HomeOwnerEmail = null,
    bool IncludeBrokerClause = false,
    ListingAddedVia AddedVia = ListingAddedVia.Manual,
    string? AddedViaDetail = null);

public sealed record HouseRulesRequest(
    TimeOnly CheckInTime,
    TimeOnly CheckOutTime,
    int MaxGuests,
    bool PetsAllowed,
    string? PetsNotes,
    bool SmokingAllowed,
    bool PartiesAllowed,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string? LeavingInstructions,
    string? AdditionalRules);

public sealed record CancellationPolicyRequest(
    CancellationPolicyType Type,
    int FreeCancellationDays,
    int? PartialRefundPercent,
    int? PartialRefundDays,
    string? CustomTerms);
