import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Plus,
  Pencil,
  Search,
  Eye,
  Rocket,
  Ban,
  Bed,
  Bath,
  ImageOff,
  Calendar,
  Sparkles,
  X,
} from "lucide-react";
import { useMyListings } from "@/features/listings/hooks/useMyListings";
import { listingApi } from "@/features/listings/services/listingApi";
import type { ListingStatus, ListingSummaryDto } from "@/api/types";
import { Button, buttonVariants } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { formatMoney, formatDate } from "@/utils/format";
import { cn } from "@/lib/utils";

const statusVariant: Record<string, "secondary" | "success" | "accent" | "outline"> = {
  Draft: "secondary",
  Published: "success",
  Activated: "accent",
  Closed: "outline",
};

type StatusFilter = "all" | ListingStatus;

const tabs: { value: StatusFilter; label: string }[] = [
  { value: "all", label: "All" },
  { value: "Draft", label: "Draft" },
  { value: "Published", label: "Published" },
  { value: "Activated", label: "Activated" },
  { value: "Closed", label: "Closed" },
];

export const MyListingsPage = () => {
  const { data, isLoading, isError } = useMyListings();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<StatusFilter>("all");
  const [search, setSearch] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);

  const items = data ?? [];

  const counts = useMemo(() => {
    const c: Record<string, number> = { all: items.length };
    for (const l of items) c[l.status] = (c[l.status] ?? 0) + 1;
    return c;
  }, [items]);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return items.filter((l) => {
      if (tab !== "all" && l.status !== tab) return false;
      if (term && !l.title.toLowerCase().includes(term)) return false;
      return true;
    });
  }, [items, tab, search]);

  const publishMutation = useMutation({
    mutationFn: (id: string) => listingApi.publish(id),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      const detail =
        (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (err instanceof Error ? err.message : "Failed to publish listing.");
      setActionError(detail);
    },
  });

  const closeMutation = useMutation({
    mutationFn: (id: string) => listingApi.close(id),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      const detail =
        (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (err instanceof Error ? err.message : "Failed to close listing.");
      setActionError(detail);
    },
  });

  const isMutating = publishMutation.isPending || closeMutation.isPending;

  if (isLoading) return <Loader label="Loading your listings..." />;
  if (isError) {
    return <p className="text-destructive">Failed to load listings.</p>;
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">My listings</h1>
          <p className="mt-1 text-muted-foreground">
            Manage your properties and publish to the marketplace.
          </p>
        </div>
        <Link to="/app/listings/new" className={cn(buttonVariants({ variant: "accent" }))}>
          <Plus className="h-4 w-4" />
          New listing
        </Link>
      </div>

      {actionError && (
        <Alert variant="destructive">
          <AlertDescription>{actionError}</AlertDescription>
        </Alert>
      )}

      {items.length === 0 ? (
        <EmptyState
          title="No listings yet"
          description="Create your first listing to appear in search results."
        >
          <Link to="/app/listings/new" className={cn(buttonVariants({ variant: "accent" }))}>
            <Plus className="h-4 w-4" />
            Create listing
          </Link>
        </EmptyState>
      ) : (
        <Tabs value={tab} onValueChange={(v) => setTab(v as StatusFilter)}>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <TabsList className="overflow-x-auto">
              {tabs.map((t) => (
                <TabsTrigger key={t.value} value={t.value} className="gap-1.5">
                  {t.label}
                  <span
                    className={cn(
                      "rounded-full px-1.5 text-[10px] font-semibold tabular-nums",
                      tab === t.value
                        ? "bg-foreground text-background"
                        : "bg-muted text-muted-foreground",
                    )}
                  >
                    {counts[t.value] ?? 0}
                  </span>
                </TabsTrigger>
              ))}
            </TabsList>

            <div className="relative w-full sm:max-w-sm">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search your listings by title..."
                className="h-10 pl-9"
              />
              {search && (
                <button
                  type="button"
                  onClick={() => setSearch("")}
                  aria-label="Clear search"
                  className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1 text-muted-foreground hover:bg-muted cursor-pointer"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
          </div>

          {tabs.map((t) => (
            <TabsContent key={t.value} value={t.value} className="mt-4">
              {filtered.length === 0 ? (
                <EmptyState
                  title={search ? "No matching listings" : `No ${t.label.toLowerCase()} listings`}
                  description={
                    search
                      ? "Try a different search term or clear filters."
                      : t.value === "Draft"
                        ? "Drafts will appear here while you finish setting them up."
                        : t.value === "Published"
                          ? "Once you publish a draft, it will show up here."
                          : t.value === "Activated"
                            ? "Activated listings have an active billing subscription."
                            : t.value === "Closed"
                              ? "Listings you close will be moved here."
                              : "Try adjusting your filter."
                  }
                />
              ) : (
                <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                  {filtered.map((l) => (
                    <ListingRowCard
                      key={l.id}
                      listing={l}
                      onPublish={() => publishMutation.mutate(l.id)}
                      onClose={() => {
                        if (window.confirm("Close this listing? It will no longer appear in search.")) {
                          closeMutation.mutate(l.id);
                        }
                      }}
                      isMutating={isMutating}
                    />
                  ))}
                </div>
              )}
            </TabsContent>
          ))}
        </Tabs>
      )}
    </div>
  );
};

function ListingRowCard({
  listing,
  onPublish,
  onClose,
  isMutating,
}: {
  listing: ListingSummaryDto;
  onPublish: () => void;
  onClose: () => void;
  isMutating: boolean;
}) {
  const canPublish = listing.status === "Draft";
  const canClose = listing.status === "Published" || listing.status === "Activated";

  return (
    <Card className="overflow-hidden transition-shadow hover:shadow-md">
      <Link
        to={`/app/listings/${listing.id}`}
        aria-label={`Open ${listing.title}`}
        className="block aspect-[16/10] overflow-hidden bg-muted relative"
      >
        {listing.coverPhotoUrl ? (
          <img
            src={listing.coverPhotoUrl}
            alt={listing.title}
            className="h-full w-full object-cover transition-transform hover:scale-[1.02]"
            loading="lazy"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center">
            <ImageOff className="h-10 w-10 text-muted-foreground/40" />
          </div>
        )}
        <div className="absolute left-3 top-3">
          <Badge variant={statusVariant[listing.status] ?? "secondary"}>{listing.status}</Badge>
        </div>
        {listing.qualityScore != null && (
          <div className="absolute right-3 top-3">
            <Badge variant="secondary" className="gap-1 bg-background/90 backdrop-blur">
              <Sparkles className="h-3 w-3" />
              {Math.round(listing.qualityScore)}
            </Badge>
          </div>
        )}
      </Link>

      <CardContent className="space-y-3 p-4">
        <div>
          <Link
            to={`/app/listings/${listing.id}`}
            className="font-semibold leading-tight line-clamp-1 hover:underline"
          >
            {listing.title}
          </Link>
          <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground">
            <span className="font-medium text-foreground">
              {formatMoney(listing.monthlyRentCents)}
              <span className="text-muted-foreground font-normal"> / mo</span>
            </span>
            <span className="flex items-center gap-1">
              <Bed className="h-3 w-3" />
              {listing.bedrooms === 0 ? "Studio" : `${listing.bedrooms} bd`}
            </span>
            <span className="flex items-center gap-1">
              <Bath className="h-3 w-3" />
              {listing.bathrooms} ba
            </span>
            <span className="flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              {formatDate(listing.createdAt)}
            </span>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Link
            to={`/app/listings/${listing.id}`}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }), "flex-1")}
          >
            <Eye className="h-4 w-4" />
            View
          </Link>
          <Link
            to={`/app/listings/${listing.id}/edit`}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }), "flex-1")}
          >
            <Pencil className="h-4 w-4" />
            Edit
          </Link>
          {canPublish && (
            <Button
              variant="accent"
              size="sm"
              onClick={onPublish}
              disabled={isMutating}
              title="Publish to marketplace"
            >
              <Rocket className="h-4 w-4" />
              Publish
            </Button>
          )}
          {canClose && (
            <Button
              variant="outline"
              size="sm"
              onClick={onClose}
              disabled={isMutating}
              title="Close listing"
            >
              <Ban className="h-4 w-4" />
              Close
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
