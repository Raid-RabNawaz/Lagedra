import { Heart } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";
import { useAuthStore } from "@/app/auth/authStore";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

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

type SaveButtonProps = {
  listingId: string;
  className?: string;
};

export function SaveButton({ listingId, className }: SaveButtonProps) {
  const user = useAuthStore((s) => s.user);
  const queryClient = useQueryClient();
  const { data: savedIds } = useSavedListingIds();

  const isSaved = savedIds?.has(listingId) ?? false;

  const { mutate: toggle, isPending } = useMutation({
    mutationFn: () =>
      isSaved
        ? listingApi.unsaveListing(listingId)
        : listingApi.saveListing(listingId),
    // Optimistically flip the heart so the click feels instant. We snapshot
    // the previous Set so we can roll back on failure.
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: savedListingIdsKey });
      const previous = queryClient.getQueryData<Set<string>>(savedListingIdsKey);
      const next = new Set(previous ?? []);
      if (isSaved) {
        next.delete(listingId);
      } else {
        next.add(listingId);
      }
      queryClient.setQueryData(savedListingIdsKey, next);
      return { previous };
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(savedListingIdsKey, context.previous);
      }
    },
    onSettled: () => {
      // Refresh ids, the user's list, and any collection counts/lists.
      void queryClient.invalidateQueries({ queryKey: SAVED_LISTINGS_ROOT });
    },
  });

  if (!user) return null;

  return (
    <Button
      variant="ghost"
      size="icon"
      disabled={isPending}
      aria-pressed={isSaved}
      aria-label={isSaved ? "Remove from saved" : "Save listing"}
      title={isSaved ? "Remove from saved" : "Save listing"}
      className={cn(
        "h-9 w-9 rounded-full bg-background/80 backdrop-blur hover:bg-background transition-colors",
        className,
      )}
      onClick={(e) => {
        e.preventDefault();
        e.stopPropagation();
        toggle();
      }}
    >
      <Heart
        className={cn(
          "h-4.5 w-4.5 transition-colors",
          isSaved ? "fill-red-500 text-red-500" : "text-foreground",
        )}
      />
    </Button>
  );
}
