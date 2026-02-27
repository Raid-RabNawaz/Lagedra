namespace Lagedra.Modules.ListingAndLocation.Presentation.Contracts;

using Lagedra.Modules.ListingAndLocation.Domain.Enums;

public sealed record UpdateListingRequest(
    PropertyType PropertyType,
    string Title,
    string Description,
    long MonthlyRentCents,
    bool InsuranceRequired,
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
    IReadOnlyList<Guid>? ConsiderationIds = null);
