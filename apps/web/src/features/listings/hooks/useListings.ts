import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";
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
