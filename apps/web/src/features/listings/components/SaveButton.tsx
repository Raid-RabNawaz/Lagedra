import { Heart } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";
import { useAuthStore } from "@/app/auth/authStore";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export function useSavedListingIds() {
  const user = useAuthStore((s) => s.user);
  return useQuery({
    queryKey: ["saved-listings"],
    queryFn: async () => {
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
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["saved-listings"] });
    },
  });

  if (!user) return null;

  return (
    <Button
      variant="ghost"
      size="icon"
      disabled={isPending}
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
