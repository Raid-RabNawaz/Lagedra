import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Plus,
  Pencil,
  Search,
  Eye,
  Send,
  Ban,
  Bed,
  Bath,
  ImageOff,
  Calendar,
  Sparkles,
  X,
  Trash2,
  Clock,
  AlertTriangle,
} from "lucide-react";
import { useMyListings } from "@/features/listings/hooks/useMyListings";
import { listingApi } from "@/features/listings/services/listingApi";
import type { ListingStatus, ListingSummaryDto } from "@/api/types";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { PageHeader } from "@/components/shared/PageHeader";
import { CardGridSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Building2 } from "lucide-react";
import { formatMoney, formatDate } from "@/utils/format";
import { cn } from "@/lib/utils";

const statusVariant: Record<string, "secondary" | "success" | "accent" | "outline" | "default" | "destructive"> = {
  Draft: "secondary",
  InReview: "default",
  Published: "success",
  Activated: "accent",
  Closed: "outline",
  Denied: "destructive",
};

const statusLabel: Record<string, string> = {
  Draft: "Draft",
  InReview: "In review",
  Published: "Published",
  Activated: "Activated",
  Closed: "Closed",
  Denied: "Needs changes",
};

type StatusFilter = "all" | ListingStatus;

const tabs: { value: StatusFilter; label: string }[] = [
  { value: "all", label: "All" },
  { value: "Draft", label: "Draft" },
  { value: "InReview", label: "In review" },
  { value: "Denied", label: "Needs changes" },
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

  // Memoise so identity is stable across renders (otherwise the `useMemo`s
  // below would recompute every render due to a fresh `[]` reference).
  const items = useMemo(() => data ?? [], [data]);

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

  const submitMutation = useMutation({
    mutationFn: (id: string) => listingApi.submitForReview(id),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      const detail =
        (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (err instanceof Error ? err.message : "Failed to submit listing for review.");
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

  const deleteMutation = useMutation({
    mutationFn: (id: string) => listingApi.delete(id),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      const detail =
        (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (err instanceof Error ? err.message : "Failed to delete listing.");
      setActionError(detail);
    },
  });

  const isMutating = submitMutation.isPending || closeMutation.isPending || deleteMutation.isPending;

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Building2}
        title="My listings"
        description="Manage your properties and publish to the marketplace."
      >
        <Link to="/app/listings/new" className={cn(buttonVariants({ variant: "accent" }))}>
          <Plus className="h-4 w-4" />
          New listing
        </Link>
      </PageHeader>

      {actionError && (
        <Alert variant="destructive">
          <AlertDescription>{actionError}</AlertDescription>
        </Alert>
      )}

      {isLoading ? (
        <CardGridSkeleton cards={6} />
      ) : isError ? (
        <ErrorState
          title="Couldn't load your listings"
          message="Something went wrong while loading your listings."
        />
      ) : items.length === 0 ? (
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
                        : t.value === "InReview"
                          ? "Listings you submit will sit here while an admin reviews them."
                          : t.value === "Denied"
                            ? "Listings the admin asked you to fix will appear here."
                            : t.value === "Published"
                              ? "Once an admin approves your submission, it will show up here."
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
                      onSubmitForReview={() => submitMutation.mutate(l.id)}
                      onClose={() => {
                        if (window.confirm("Close this listing? It will no longer appear in search.")) {
                          closeMutation.mutate(l.id);
                        }
                      }}
                      onDelete={() => {
                        if (window.confirm("Delete this listing permanently? This cannot be undone.")) {
                          deleteMutation.mutate(l.id);
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
  onSubmitForReview,
  onClose,
  onDelete,
  isMutating,
}: {
  listing: ListingSummaryDto;
  onSubmitForReview: () => void;
  onClose: () => void;
  onDelete: () => void;
  isMutating: boolean;
}) {
  const canSubmit = listing.status === "Draft" || listing.status === "Denied";
  const canClose = listing.status === "Published" || listing.status === "Activated";
  const canDelete = listing.status === "Draft" || listing.status === "Denied";
  const submitLabel = listing.status === "Denied" ? "Resubmit" : "Submit for review";

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
          <Badge variant={statusVariant[listing.status] ?? "secondary"}>
            {listing.status === "InReview" && <Clock className="h-3 w-3" />}
            {listing.status === "Denied" && <AlertTriangle className="h-3 w-3" />}
            {statusLabel[listing.status] ?? listing.status}
          </Badge>
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
          {canSubmit && (
            <Button
              variant="accent"
              size="sm"
              onClick={onSubmitForReview}
              disabled={isMutating}
              title="Submit to admins for review"
            >
              <Send className="h-4 w-4" />
              {submitLabel}
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
          {canDelete && (
            <Button
              variant="ghost"
              size="sm"
              onClick={onDelete}
              disabled={isMutating}
              title="Delete listing permanently"
              className="text-destructive hover:text-destructive"
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
