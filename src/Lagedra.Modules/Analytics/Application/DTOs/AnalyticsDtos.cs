namespace Lagedra.Modules.Analytics.Application.DTOs;

public sealed record PlatformSummaryDto(
    int TotalListings,
    int ActiveDeals,
    long MrrCents,
    double ConversionRatePercent,
    DateTime PeriodStart,
    DateTime PeriodEnd);

public sealed record ListingAnalyticsItemDto(
    Guid ListingId,
    string Title,
    int Views,
    int ApplicationCount,
    double ConversionPercent,
    double QualityScore);
