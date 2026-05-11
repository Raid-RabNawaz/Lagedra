import { useQuery } from "@tanstack/react-query";
import { useAuthStore } from "@/app/auth/authStore";
import { listingApi } from "@/features/listings/services/listingApi";

// Query-key contract used by everything that reads/writes the user's saved
// listings. The first segment is shared so a single
// `invalidateQueries({ queryKey: SAVED_LISTINGS_ROOT })` after a mutation
// refreshes both the heart-icon cache and the SavedListingsPage list.
export const SAVED_LISTINGS_ROOT = ["saved-listings"] as const;
export const savedListingIdsKey = [...SAVED_LISTINGS_ROOT, "ids"] as const;
export const savedListingsListKey = [...SAVED_LISTINGS_ROOT, "list"] as const;
export const savedCollectionsKey = [...SAVED_LISTINGS_ROOT, "collections"] as const;
export const savedCollectionListingsKey = (collectionId: string) =>
  [...SAVED_LISTINGS_ROOT, "collections", collectionId, "listings"] as const;

export function useSavedListingIds() {
  const user = useAuthStore((s) => s.user);
  return useQuery<Set<string>>({
    queryKey: savedListingIdsKey,
    queryFn: async () => {
      // 200 covers the vast majority of real users; if a user exceeds this we
      // still get correct behaviour for the most-recent saves and the
      // SavedListingsPage paginates the rest.
      const listings = await listingApi.getSavedListings(1, 200);
      return new Set(listings.map((l) => l.id));
    },
    enabled: Boolean(user),
    staleTime: 60_000,
  });
}
