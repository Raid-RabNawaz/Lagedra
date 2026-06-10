import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";
import { privacyApi } from "@/features/privacy/services/privacyApi";
import type { SearchListingsParams } from "@/api/types";

export function useListings(params: SearchListingsParams) {
  return useQuery({
    queryKey: ["listings", params],
    queryFn: () => listingApi.search(params),
    placeholderData: keepPreviousData,
    staleTime: 60_000,
  });
}

export function useListingDetail(id: string | undefined) {
  return useQuery({
    queryKey: ["listing", id],
    queryFn: () => listingApi.getDetail(id!),
    enabled: Boolean(id),
    staleTime: 120_000,
  });
}

export function useSimilarListings(id: string | undefined) {
  return useQuery({
    queryKey: ["listings", "similar", id],
    queryFn: () => listingApi.getSimilar(id!),
    enabled: Boolean(id),
    staleTime: 120_000,
  });
}

/**
 * Range-aware availability for a listing — used by the Phase 16 Listing
 * Detail booking widget to gate the price quote and Apply CTA.
 */
export function useListingAvailabilityRange(
  listingId: string | undefined,
  from: string | undefined,
  to: string | undefined,
) {
  return useQuery({
    queryKey: ["listing", "availability", listingId, from, to],
    queryFn: () => listingApi.getAvailabilityRange(listingId!, from!, to!),
    enabled: Boolean(listingId && from && to),
    staleTime: 30_000,
    retry: false,
  });
}

/** Phase 16 itemised quote for a listing + stay window. */
export function useListingQuote(
  listingId: string | undefined,
  checkIn: string | undefined,
  checkOut: string | undefined,
  enabled: boolean = true,
) {
  return useQuery({
    queryKey: ["listing", "quote", listingId, checkIn, checkOut],
    queryFn: () => listingApi.getQuote(listingId!, checkIn!, checkOut!),
    enabled: Boolean(enabled && listingId && checkIn && checkOut),
    staleTime: 60_000,
    retry: false,
  });
}

/**
 * Booking pre-flight: returns whether the signed-in caller has every
 * required consent (KYC + DataProcessing). Drives the Listing Detail KYC
 * banner and is safe to call without consent (endpoint is exempt from
 * the global consent middleware).
 */
export function useMyConsentStatus(enabled: boolean = true) {
  return useQuery({
    queryKey: ["privacy", "my-consent-status"],
    queryFn: () => privacyApi.getMyConsentStatus(),
    enabled,
    staleTime: 60_000,
  });
}
