import { Heart } from "lucide-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";
import {
  SAVED_LISTINGS_ROOT,
  savedListingIdsKey,
  useSavedListingIds,
} from "@/features/listings/hooks/useSavedListings";
import { useAuthStore } from "@/app/auth/authStore";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

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
