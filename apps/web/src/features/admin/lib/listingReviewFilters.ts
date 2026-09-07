import type { ListingReviewItemDto, PropertyType } from "@/api/types";
import { MIN_HOST_PROFILE_COMPLETENESS } from "@/features/auth/lib/profileCompleteness";

export type ListingReviewTriState = "any" | "yes" | "no";

export type ListingReviewFilters = {
  location: string;
  hostName: string;
  propertyType: PropertyType | "";
  title: string;
  lease: "any" | "custom" | "standard";
  instantBooking: ListingReviewTriState;
  hostIdVerified: ListingReviewTriState;
  hostProfile: "any" | "incomplete";
};

export const emptyListingReviewFilters = (): ListingReviewFilters => ({
  location: "",
  hostName: "",
  propertyType: "",
  title: "",
  lease: "any",
  instantBooking: "any",
  hostIdVerified: "any",
  hostProfile: "any",
});

export function listingReviewLocationLabel(item: Pick<ListingReviewItemDto, "city" | "state" | "country">): string {
  const parts = [item.city, item.state].map((p) => p?.trim()).filter(Boolean);
  if (parts.length === 0) {
    return item.country?.trim() || "";
  }
  if (item.country?.trim() && item.country.trim() !== "US" && item.country.trim() !== "USA") {
    parts.push(item.country.trim());
  }
  return parts.join(", ");
}

export function listingReviewHasActiveFilters(filters: ListingReviewFilters): boolean {
  return (
    Boolean(filters.location.trim()) ||
    Boolean(filters.hostName.trim()) ||
    Boolean(filters.propertyType) ||
    Boolean(filters.title.trim()) ||
    filters.lease !== "any" ||
    filters.instantBooking !== "any" ||
    filters.hostIdVerified !== "any" ||
    filters.hostProfile !== "any"
  );
}

function includesNormalized(haystack: string | null | undefined, needle: string): boolean {
  if (!needle) return true;
  return (haystack ?? "").toLowerCase().includes(needle);
}

function matchesTriState(value: boolean, filter: ListingReviewTriState): boolean {
  if (filter === "any") return true;
  return filter === "yes" ? value : !value;
}

export function filterListingReviewItems(
  items: ListingReviewItemDto[],
  filters: ListingReviewFilters,
): ListingReviewItemDto[] {
  const location = filters.location.trim().toLowerCase();
  const hostName = filters.hostName.trim().toLowerCase();
  const title = filters.title.trim().toLowerCase();

  return items.filter((item) => {
    if (filters.propertyType && item.propertyType !== filters.propertyType) {
      return false;
    }

    if (title && !includesNormalized(item.title, title)) {
      return false;
    }

    if (hostName && !includesNormalized(item.hostDisplayName, hostName)) {
      return false;
    }

    if (location) {
      const haystack = [item.city, item.state, item.country, listingReviewLocationLabel(item)]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      if (!haystack.includes(location)) {
        return false;
      }
    }

    if (filters.lease === "custom" && item.usesCustomLeaseAgreement !== true) {
      return false;
    }
    if (filters.lease === "standard" && item.usesCustomLeaseAgreement === true) {
      return false;
    }

    if (!matchesTriState(item.instantBookingEnabled ?? false, filters.instantBooking)) {
      return false;
    }

    if (!matchesTriState(item.hostIsGovernmentIdVerified, filters.hostIdVerified)) {
      return false;
    }

    if (
      filters.hostProfile === "incomplete" &&
      item.hostProfileCompletenessPercent >= MIN_HOST_PROFILE_COMPLETENESS
    ) {
      return false;
    }

    return true;
  });
}
