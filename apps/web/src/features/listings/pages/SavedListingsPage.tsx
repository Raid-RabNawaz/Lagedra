import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, FolderMinus, Heart, Plus, X } from "lucide-react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingCard } from "@/features/listings/components/ListingCard";
import {
  SAVED_LISTINGS_ROOT,
  savedCollectionListingsKey,
  savedCollectionsKey,
  savedListingsListKey,
} from "@/features/listings/hooks/useSavedListings";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { cn } from "@/lib/utils";
import { extractErrorMessage } from "@/lib/errors";
import type { ListingSummaryDto, SavedListingCollectionDto } from "@/api/types";

export const SavedListingsPage = () => {
  const queryClient = useQueryClient();
  const [activeCollection, setActiveCollection] = useState<string | null>(null);
  const [showNewCollection, setShowNewCollection] = useState(false);
  const [newCollectionName, setNewCollectionName] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);

  const invalidateAllSaved = () =>
    void queryClient.invalidateQueries({ queryKey: SAVED_LISTINGS_ROOT });

  const {
    data: listings,
    isLoading,
    isError,
    error: listingsError,
    refetch: refetchListings,
  } = useQuery({
    queryKey: savedListingsListKey,
    queryFn: () => listingApi.getSavedListings(),
    staleTime: 60_000,
  });

  const { data: collections } = useQuery({
    queryKey: savedCollectionsKey,
    queryFn: () => listingApi.getCollections(),
  });

  const {
    data: collectionListings,
    isLoading: isCollectionLoading,
    isError: isCollectionError,
    error: collectionListingsError,
    refetch: refetchCollectionListings,
  } = useQuery({
    queryKey: activeCollection
      ? savedCollectionListingsKey(activeCollection)
      : [...SAVED_LISTINGS_ROOT, "collections", "none", "listings"],
    queryFn: () => listingApi.getCollectionListings(activeCollection!),
    enabled: !!activeCollection,
  });

  const createCollectionMutation = useMutation({
    mutationFn: (name: string) => listingApi.createCollection(name),
    onSuccess: () => {
      setActionError(null);
      invalidateAllSaved();
      setNewCollectionName("");
      setShowNewCollection(false);
    },
    onError: (err) => setActionError(extractErrorMessage(err)),
  });

  const addToCollectionMutation = useMutation({
    mutationFn: ({ listingId, collectionId }: { listingId: string; collectionId: string }) =>
      listingApi.addToCollection(listingId, collectionId),
    onSuccess: () => {
      setActionError(null);
      invalidateAllSaved();
    },
    onError: (err) => setActionError(extractErrorMessage(err)),
  });

  const removeFromCollectionMutation = useMutation({
    mutationFn: (listingId: string) => listingApi.removeFromCollection(listingId),
    onSuccess: () => {
      setActionError(null);
      invalidateAllSaved();
    },
    onError: (err) => setActionError(extractErrorMessage(err)),
  });

  const unsaveMutation = useMutation({
    mutationFn: (listingId: string) => listingApi.unsaveListing(listingId),
    onSuccess: () => {
      setActionError(null);
      invalidateAllSaved();
    },
    onError: (err) => setActionError(extractErrorMessage(err)),
  });

  // Defensive coercion against any non-array shape sneaking through (e.g. an
  // unexpected error body) — we don't want a single bad response to crash the
  // whole page.
  const safeListings: ListingSummaryDto[] = Array.isArray(listings) ? listings : [];
  const safeCollectionListings: ListingSummaryDto[] = Array.isArray(collectionListings)
    ? collectionListings
    : [];
  const safeCollections: SavedListingCollectionDto[] = Array.isArray(collections) ? collections : [];

  const displayedListings: ListingSummaryDto[] = activeCollection
    ? safeCollectionListings
    : safeListings;
  const loading = activeCollection ? isCollectionLoading : isLoading;
  const error = activeCollection ? isCollectionError : isError;
  const errorObject = activeCollection ? collectionListingsError : listingsError;
  const handleRetry = activeCollection
    ? () => void refetchCollectionListings()
    : () => void refetchListings();

  // Hide the active collection from the "move to" dropdown — selecting the
  // current collection is a confusing no-op.
  const moveTargets = safeCollections.filter((c) => c.id !== activeCollection);

  return (
    <div>
      <div className="mb-6">
        <Link
          to="/listings"
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-3"
        >
          <ArrowLeft className="h-4 w-4" />
          Browse listings
        </Link>
        <h1 className="text-2xl font-bold tracking-tight">Saved listings</h1>
      </div>

      <div className="flex flex-wrap items-center gap-2 mb-6">
        <Button
          variant={!activeCollection ? "default" : "outline"}
          size="sm"
          onClick={() => setActiveCollection(null)}
        >
          All saved
          {safeListings.length > 0 && (
            <Badge variant="secondary" className="ml-1.5 text-[10px]">
              {safeListings.length}
            </Badge>
          )}
        </Button>
        {safeCollections.map((c) => (
          <Button
            key={c.id}
            variant={activeCollection === c.id ? "default" : "outline"}
            size="sm"
            onClick={() => setActiveCollection(c.id)}
          >
            {c.name}
            <Badge variant="secondary" className="ml-1.5 text-[10px]">
              {c.listingCount}
            </Badge>
          </Button>
        ))}

        {showNewCollection ? (
          <form
            className="flex items-center gap-1.5"
            onSubmit={(e) => {
              e.preventDefault();
              const trimmed = newCollectionName.trim();
              if (trimmed) createCollectionMutation.mutate(trimmed);
            }}
          >
            <Input
              autoFocus
              placeholder="Collection name"
              value={newCollectionName}
              onChange={(e) => setNewCollectionName(e.target.value)}
              maxLength={100}
              className="h-9 w-40 text-sm"
            />
            <Button
              type="submit"
              size="sm"
              disabled={!newCollectionName.trim() || createCollectionMutation.isPending}
            >
              {createCollectionMutation.isPending ? "Creating…" : "Create"}
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="h-9 w-9"
              onClick={() => {
                setShowNewCollection(false);
                setNewCollectionName("");
              }}
            >
              <X className="h-4 w-4" />
            </Button>
          </form>
        ) : (
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowNewCollection(true)}
          >
            <Plus className="h-4 w-4" />
            New collection
          </Button>
        )}
      </div>

      {actionError && (
        <div className="mb-4 flex items-start justify-between gap-3 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          <span>{actionError}</span>
          <button
            type="button"
            onClick={() => setActionError(null)}
            aria-label="Dismiss error"
            className="text-destructive/70 hover:text-destructive"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {loading ? (
        <Loader label="Loading saved listings..." />
      ) : error ? (
        <ErrorState
          title="Couldn't load saved listings"
          error={errorObject}
          onRetry={handleRetry}
        />
      ) : displayedListings.length > 0 ? (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {displayedListings.map((listing) => (
            <div key={listing.id} className="space-y-1.5">
              <ListingCard listing={listing} />
              <div className="flex items-center gap-1.5">
                {moveTargets.length > 0 && (
                  <Select
                    className="h-8 flex-1 text-xs"
                    value=""
                    aria-label={`Move ${listing.title} to a collection`}
                    onChange={(e) => {
                      if (e.target.value) {
                        addToCollectionMutation.mutate({
                          listingId: listing.id,
                          collectionId: e.target.value,
                        });
                      }
                    }}
                  >
                    <option value="">
                      {activeCollection ? "Move to collection…" : "Add to collection…"}
                    </option>
                    {moveTargets.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </Select>
                )}
                {activeCollection ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="h-8 px-2"
                    title="Remove from this collection"
                    aria-label={`Remove ${listing.title} from this collection`}
                    disabled={removeFromCollectionMutation.isPending}
                    onClick={() => removeFromCollectionMutation.mutate(listing.id)}
                  >
                    <FolderMinus className="h-3.5 w-3.5" />
                  </Button>
                ) : (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="h-8 px-2 text-destructive hover:text-destructive"
                    title="Remove from saved"
                    aria-label={`Remove ${listing.title} from saved`}
                    disabled={unsaveMutation.isPending}
                    onClick={() => unsaveMutation.mutate(listing.id)}
                  >
                    <Heart className="h-3.5 w-3.5 fill-current" />
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          title={activeCollection ? "No listings in this collection" : "No saved listings"}
          description={
            activeCollection
              ? "Add listings to this collection from the 'All saved' tab."
              : "Browse listings and tap the heart icon to save ones you like."
          }
        >
          {!activeCollection && (
            <Link
              to="/listings"
              className={cn(buttonVariants({ variant: "outline" }), "mt-2")}
            >
              Browse listings
            </Link>
          )}
        </EmptyState>
      )}
    </div>
  );
};
