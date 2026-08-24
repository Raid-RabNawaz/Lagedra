import type { ListingAnalyticsItemDto, PlatformSummaryDto } from "@/api/types";
import { centsToUsd, downloadCsv, isoDate, toCsv } from "./csv";

export function buildPlatformAnalyticsCsv(summary: PlatformSummaryDto): string {
  return toCsv(
    [
      "periodStart",
      "periodEnd",
      "totalListings",
      "listingsAdded",
      "applications",
      "newDeals",
      "activeDeals",
      "mrrCents",
      "mrrUsd",
      "conversionRatePercent",
    ],
    [
      [
        isoDate(summary.periodStart),
        isoDate(summary.periodEnd),
        summary.totalListings,
        summary.listingsAdded,
        summary.totalApplications,
        summary.newDeals,
        summary.activeDeals,
        summary.mrrCents,
        centsToUsd(summary.mrrCents),
        summary.conversionRatePercent.toFixed(1),
      ],
    ],
  );
}

export function platformAnalyticsFilename(summary: PlatformSummaryDto): string {
  return `lagedra-platform-analytics-${isoDate(summary.periodStart)}-to-${isoDate(summary.periodEnd)}.csv`;
}

export function buildListingAnalyticsCsv(items: readonly ListingAnalyticsItemDto[]): string {
  return toCsv(
    [
      "listingId",
      "title",
      "landlordUserId",
      "landlordName",
      "landlordEmail",
      "status",
      "added",
      "monthlyRentCents",
      "monthlyRentUsd",
      "applicationCount",
      "conversionPercent",
      "qualityScore",
      "addedVia",
    ],
    items.map((item) => [
      item.listingId,
      item.title,
      item.landlordUserId,
      item.landlordName,
      item.landlordEmail,
      item.status,
      isoDate(item.createdAt),
      item.monthlyRentCents,
      centsToUsd(item.monthlyRentCents),
      item.applicationCount,
      item.conversionPercent.toFixed(1),
      item.qualityScore,
      item.addedVia,
    ]),
  );
}

export function listingAnalyticsFilename(today = new Date()): string {
  return `lagedra-listing-analytics-${today.toISOString().slice(0, 10)}.csv`;
}

export function downloadPlatformAnalyticsReport(summary: PlatformSummaryDto): void {
  downloadCsv(platformAnalyticsFilename(summary), buildPlatformAnalyticsCsv(summary));
}

export function downloadListingAnalyticsReport(items: readonly ListingAnalyticsItemDto[]): void {
  downloadCsv(listingAnalyticsFilename(), buildListingAnalyticsCsv(items));
}
