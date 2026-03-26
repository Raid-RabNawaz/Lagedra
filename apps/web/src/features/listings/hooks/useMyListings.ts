import { useQuery } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";

export function useMyListings() {
  return useQuery({
    queryKey: ["listings", "mine"],
    queryFn: () => listingApi.getMine(),
  });
}
