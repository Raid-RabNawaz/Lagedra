import { useState } from "react";
import { Heart } from "lucide-react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";
import {
  SAVED_LISTINGS_ROOT,
  savedListingIdsKey,
  useSavedListingIds,
} from "@/features/listings/hooks/useSavedListings";
import { useAuthStore } from "@/app/auth/authStore";
import { SignInDialog } from "@/features/auth/components/SignInDialog";
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
  const [signInOpen, setSignInOpen] = useState(false);

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

  const saveAfterSignIn = async () => {
    // Guest clicked the heart intending to save — complete that after auth.
    try {
      await listingApi.saveListing(listingId);
      const previous = queryClient.getQueryData<Set<string>>(savedListingIdsKey);
      const next = new Set(previous ?? []);
      next.add(listingId);
      queryClient.setQueryData(savedListingIdsKey, next);
    } finally {
      void queryClient.invalidateQueries({ queryKey: SAVED_LISTINGS_ROOT });
    }
  };

  return (
    <>
      <Button
        variant="ghost"
        size="icon"
        disabled={isPending}
        aria-pressed={user ? isSaved : false}
        aria-label={
          !user
            ? "Sign in to save listing"
            : isSaved
              ? "Remove from saved"
              : "Save listing"
        }
        title={
          !user
            ? "Sign in to save"
            : isSaved
              ? "Remove from saved"
              : "Save listing"
        }
        className={cn(
          "h-9 w-9 rounded-full bg-background/80 backdrop-blur hover:bg-background transition-colors",
          className,
        )}
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          if (!user) {
            setSignInOpen(true);
            return;
          }
          toggle();
        }}
      >
        <Heart
          className={cn(
            "h-4.5 w-4.5 transition-colors",
            user && isSaved ? "fill-red-500 text-red-500" : "text-foreground",
          )}
        />
      </Button>

      <SignInDialog
        open={signInOpen}
        onOpenChange={setSignInOpen}
        title="Sign in to save"
        description="Sign in to save this listing and find it later under Saved."
        onSuccess={saveAfterSignIn}
      />
    </>
  );
}
