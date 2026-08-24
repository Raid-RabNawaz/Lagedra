import { describe, expect, it } from "vitest";
import type { ListingAnalyticsItemDto, PlatformSummaryDto } from "@/api/types";
import { csvCell, toCsv } from "./csv";
import {
  buildListingAnalyticsCsv,
  buildPlatformAnalyticsCsv,
  listingAnalyticsFilename,
  platformAnalyticsFilename,
} from "./analyticsReports";

const summary: PlatformSummaryDto = {
  totalListings: 42,
  listingsAdded: 3,
  activeDeals: 8,
  newDeals: 2,
  totalApplications: 11,
  mrrCents: 450050,
  conversionRatePercent: 18.1818,
  periodStart: "2026-08-01T00:00:00.000Z",
  periodEnd: "2026-08-19T23:59:59.000Z",
};

const listing: ListingAnalyticsItemDto = {
  listingId: "3f2a9c1e-8b14-4d6a-9e21-0c8f4b7a1d22",
  title: "Loft, downtown",
  landlordUserId: "7c8e1a44-2b09-4f11-a3d6-5e9c0b1f2a88",
  landlordName: "Ada Lovelace",
  landlordEmail: "ada@example.com",
  status: "Published",
  createdAt: "2026-07-04T15:00:00.000Z",
  monthlyRentCents: 320000,
  applicationCount: 4,
  conversionPercent: 25,
  qualityScore: 80,
  addedVia: "Hostaway",
};

describe("csvCell", () => {
  it("quotes commas, quotes, and line breaks", () => {
    expect(csvCell("Loft, downtown")).toBe("\"Loft, downtown\"");
    expect(csvCell("Say \"hello\"")).toBe("\"Say \"\"hello\"\"\"");
    expect(csvCell("line\nbreak")).toBe("\"line\nbreak\"");
  });

  it("leaves plain values unquoted", () => {
    expect(csvCell("Published")).toBe("Published");
    expect(csvCell(12)).toBe("12");
    expect(csvCell(null)).toBe("");
  });
});

describe("toCsv", () => {
  it("uses CRLF and a trailing newline", () => {
    expect(toCsv(["a", "b"], [[1, 2]])).toBe("a,b\r\n1,2\r\n");
  });
});

describe("analytics reports", () => {
  it("builds a one-row platform summary with period dates and dollars", () => {
    const csv = buildPlatformAnalyticsCsv(summary);
    expect(csv).toContain("periodStart,periodEnd,totalListings");
    expect(csv).toContain("2026-08-01,2026-08-19,42,3,11,2,8,450050,4500.50,18.2");
    expect(platformAnalyticsFilename(summary)).toBe(
      "lagedra-platform-analytics-2026-08-01-to-2026-08-19.csv",
    );
  });

  it("builds listing rows matching the on-screen columns", () => {
    const csv = buildListingAnalyticsCsv([listing]);
    expect(csv).toContain("listingId,title,landlordUserId");
    expect(csv).toContain("addedVia");
    expect(csv).toContain("\"Loft, downtown\"");
    expect(csv).toContain("2026-07-04,320000,3200.00,4,25.0,80,Hostaway");
    expect(listingAnalyticsFilename(new Date("2026-08-19T10:00:00.000Z"))).toBe(
      "lagedra-listing-analytics-2026-08-19.csv",
    );
  });
});
