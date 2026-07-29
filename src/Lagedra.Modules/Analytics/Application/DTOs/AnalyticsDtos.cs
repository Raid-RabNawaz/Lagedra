namespace Lagedra.Modules.Analytics.Application.DTOs;

public sealed record PlatformSummaryDto(
    int TotalListings,
    int ListingsAdded,
    int ActiveDeals,
    int NewDeals,
    int TotalApplications,
    long MrrCents,
    double ConversionRatePercent,
    DateTime PeriodStart,
    DateTime PeriodEnd);

public sealed record ListingAnalyticsItemDto(
    Guid ListingId,
    string Title,
    Guid LandlordUserId,
    string LandlordName,
    string? LandlordEmail,
    string Status,
    DateTime CreatedAt,
    long MonthlyRentCents,
    int ApplicationCount,
    double ConversionPercent,
    double QualityScore);
