import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, Plus, X } from "lucide-react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingCard } from "@/features/listings/components/ListingCard";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { Button, buttonVariants } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { cn } from "@/lib/utils";
import type { ListingSummaryDto } from "@/api/types";

export const SavedListingsPage = () => {
  const queryClient = useQueryClient();
  const [activeCollection, setActiveCollection] = useState<string | null>(null);
  const [showNewCollection, setShowNewCollection] = useState(false);
  const [newCollectionName, setNewCollectionName] = useState("");

  const { data: listings, isLoading, isError } = useQuery({
    queryKey: ["saved-listings"],
    queryFn: () => listingApi.getSavedListings(),
    staleTime: 60_000,
  });

  const { data: collections } = useQuery({
    queryKey: ["collections"],
    queryFn: () => listingApi.getCollections(),
  });

  const {
    data: collectionListings,
    isLoading: isCollectionLoading,
    isError: isCollectionError,
  } = useQuery({
    queryKey: ["collection-listings", activeCollection],
    queryFn: () => listingApi.getCollectionListings(activeCollection!),
    enabled: !!activeCollection,
  });

  const createCollectionMutation = useMutation({
    mutationFn: (name: string) => listingApi.createCollection(name),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["collections"] });
      setNewCollectionName("");
      setShowNewCollection(false);
    },
  });

  const addToCollectionMutation = useMutation({
    mutationFn: ({ listingId, collectionId }: { listingId: string; collectionId: string }) =>
      listingApi.addToCollection(listingId, collectionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["collections"] });
      if (activeCollection) {
        void queryClient.invalidateQueries({ queryKey: ["collection-listings", activeCollection] });
      }
    },
  });

  const displayedListings: ListingSummaryDto[] | undefined = activeCollection
    ? collectionListings
    : listings;
  const loading = activeCollection ? isCollectionLoading : isLoading;
  const error = activeCollection ? isCollectionError : isError;

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
        </Button>
        {collections?.map((c) => (
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
              className="h-9 w-40 text-sm"
            />
            <Button
              type="submit"
              size="sm"
              disabled={!newCollectionName.trim() || createCollectionMutation.isPending}
            >
              Create
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

      {loading ? (
        <Loader label="Loading saved listings..." />
      ) : error ? (
        <div className="py-16 text-center">
          <p className="text-destructive font-medium">Failed to load saved listings.</p>
        </div>
      ) : displayedListings && displayedListings.length > 0 ? (
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {displayedListings.map((listing) => (
            <div key={listing.id} className="space-y-1">
              <ListingCard listing={listing} />
              {collections && collections.length > 0 && (
                <Select
                  className="h-8 text-xs"
                  value=""
                  onChange={(e) => {
                    if (e.target.value) {
                      addToCollectionMutation.mutate({
                        listingId: listing.id,
                        collectionId: e.target.value,
                      });
                    }
                  }}
                >
                  <option value="">Add to collection…</option>
                  {collections.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </Select>
              )}
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
