using Lagedra.Modules.ListingAndLocation.Domain.Enums;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record ListingSummaryDto(
    Guid Id,
    string Title,
    ListingStatus Status,
    PropertyType PropertyType,
    long MonthlyRentCents,
    bool InsuranceRequired,
    int Bedrooms,
    decimal Bathrooms,
    int? MinStayDays,
    int? MaxStayDays,
    double? Latitude,
    double? Longitude,
    Uri? CoverPhotoUrl,
    int? QualityScore,
    DateTime CreatedAt);
