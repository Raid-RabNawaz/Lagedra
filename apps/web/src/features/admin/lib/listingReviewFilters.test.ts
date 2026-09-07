import { describe, expect, it } from "vitest";
import type { ListingReviewItemDto } from "@/api/types";
import { MIN_HOST_PROFILE_COMPLETENESS } from "@/features/auth/lib/profileCompleteness";
import {
  emptyListingReviewFilters,
  filterListingReviewItems,
  listingReviewHasActiveFilters,
  listingReviewLocationLabel,
  type ListingReviewFilters,
} from "./listingReviewFilters";

function item(overrides: Partial<ListingReviewItemDto> = {}): ListingReviewItemDto {
  return {
    id: "listing-1",
    landlordUserId: "host-1",
    title: "Sunny loft downtown",
    propertyType: "Loft",
    bedrooms: 1,
    bathrooms: 1,
    monthlyRentCents: 250000,
    photoCount: 4,
    createdAt: "2026-08-01T00:00:00.000Z",
    hostDisplayName: "Ada Lovelace",
    hostIsGovernmentIdVerified: true,
    hostIsPhoneVerified: true,
    hostProfileCompletenessPercent: 90,
    city: "San Francisco",
    state: "CA",
    country: "US",
    instantBookingEnabled: false,
    usesCustomLeaseAgreement: false,
    ...overrides,
  };
}

function filters(overrides: Partial<ListingReviewFilters> = {}): ListingReviewFilters {
  return { ...emptyListingReviewFilters(), ...overrides };
}

describe("listingReviewLocationLabel", () => {
  it("joins city and state", () => {
    expect(listingReviewLocationLabel(item())).toBe("San Francisco, CA");
  });

  it("omits US country and includes others", () => {
    expect(listingReviewLocationLabel(item({ country: "Canada" }))).toBe(
      "San Francisco, CA, Canada",
    );
  });

  it("falls back to country when city and state are missing", () => {
    expect(listingReviewLocationLabel(item({ city: null, state: null, country: "US" }))).toBe(
      "US",
    );
  });
});

describe("listingReviewHasActiveFilters", () => {
  it("is false for empty filters", () => {
    expect(listingReviewHasActiveFilters(emptyListingReviewFilters())).toBe(false);
  });

  it("is true when any filter is set", () => {
    expect(listingReviewHasActiveFilters(filters({ location: "sf" }))).toBe(true);
    expect(listingReviewHasActiveFilters(filters({ propertyType: "House" }))).toBe(true);
    expect(listingReviewHasActiveFilters(filters({ lease: "custom" }))).toBe(true);
  });
});

describe("filterListingReviewItems", () => {
  const queue = [
    item(),
    item({
      id: "listing-2",
      title: "Quiet cottage",
      propertyType: "Cottage",
      hostDisplayName: "Grace Hopper",
      city: "Portland",
      state: "OR",
      instantBookingEnabled: true,
      usesCustomLeaseAgreement: true,
      hostIsGovernmentIdVerified: false,
      hostProfileCompletenessPercent: 40,
    }),
  ];

  it("returns every item when filters are empty", () => {
    expect(filterListingReviewItems(queue, emptyListingReviewFilters())).toHaveLength(2);
  });

  it("filters by location against city, state, or formatted label", () => {
    expect(filterListingReviewItems(queue, filters({ location: "portland" })).map((i) => i.id)).toEqual([
      "listing-2",
    ]);
    expect(filterListingReviewItems(queue, filters({ location: "CA" })).map((i) => i.id)).toEqual([
      "listing-1",
    ]);
  });

  it("filters by host name", () => {
    expect(filterListingReviewItems(queue, filters({ hostName: "hopper" })).map((i) => i.id)).toEqual([
      "listing-2",
    ]);
  });

  it("filters by listing type", () => {
    expect(filterListingReviewItems(queue, filters({ propertyType: "Loft" })).map((i) => i.id)).toEqual([
      "listing-1",
    ]);
  });

  it("filters by title, lease source, instant booking, ID, and incomplete profile", () => {
    expect(filterListingReviewItems(queue, filters({ title: "cottage" })).map((i) => i.id)).toEqual([
      "listing-2",
    ]);
    expect(filterListingReviewItems(queue, filters({ lease: "custom" })).map((i) => i.id)).toEqual([
      "listing-2",
    ]);
    expect(filterListingReviewItems(queue, filters({ instantBooking: "yes" })).map((i) => i.id)).toEqual([
      "listing-2",
    ]);
    expect(filterListingReviewItems(queue, filters({ hostIdVerified: "no" })).map((i) => i.id)).toEqual([
      "listing-2",
    ]);
    expect(
      filterListingReviewItems(queue, filters({ hostProfile: "incomplete" })).map((i) => i.id),
    ).toEqual(["listing-2"]);
    expect(queue[1]!.hostProfileCompletenessPercent).toBeLessThan(MIN_HOST_PROFILE_COMPLETENESS);
  });

  it("treats a missing custom-lease flag as the Lagedra template", () => {
    const withoutFlag = item({ id: "listing-3", usesCustomLeaseAgreement: undefined });
    expect(
      filterListingReviewItems([withoutFlag], filters({ lease: "standard" })).map((i) => i.id),
    ).toEqual(["listing-3"]);
    expect(filterListingReviewItems([withoutFlag], filters({ lease: "custom" }))).toHaveLength(0);
  });
});
