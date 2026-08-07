import { useEffect, useMemo, useState } from "react";
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
  Loader2,
} from "lucide-react";
import { useMyListings } from "@/features/listings/hooks/useMyListings";
import { listingApi } from "@/features/listings/services/listingApi";
import type { ListingStatus, ListingSummaryDto } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { FilterTabs } from "@/components/shared/FilterTabs";
import { PageHeader } from "@/components/shared/PageHeader";
import { CardGridSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Building2, CheckCircle2 } from "lucide-react";
import { HostChannelSyncButton } from "@/features/channels/components/HostChannelSyncButton";
import { formatMoney, formatDate } from "@/utils/format";
import { cn } from "@/lib/utils";

function canSubmitForReview(status: ListingStatus): boolean {
  return status === "Draft" || status === "Denied";
}

type BulkSubmitFailure = {
  id: string;
  title: string;
  message: string;
};

type BulkSubmitOutcome = {
  succeeded: number;
  failures: BulkSubmitFailure[];
};

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
  const [syncNote, setSyncNote] = useState<string | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set());
  const [bulkProgress, setBulkProgress] = useState<{ done: number; total: number } | null>(null);
  const [bulkOutcome, setBulkOutcome] = useState<BulkSubmitOutcome | null>(null);

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

  const selectableFiltered = useMemo(
    () => filtered.filter((l) => canSubmitForReview(l.status)),
    [filtered],
  );

  // Drop selections that are no longer eligible (status changed, filtered out,
  // or deleted) so the toolbar never claims to act on invisible listings.
  useEffect(() => {
    const eligible = new Set(selectableFiltered.map((l) => l.id));
    setSelectedIds((prev) => {
      let changed = false;
      const next = new Set<string>();
      for (const id of prev) {
        if (eligible.has(id)) next.add(id);
        else changed = true;
      }
      return changed ? next : prev;
    });
  }, [selectableFiltered]);

  const activeTabLabel = tabs.find((t) => t.value === tab)?.label ?? "All";
  const allSelectableSelected =
    selectableFiltered.length > 0 && selectableFiltered.every((l) => selectedIds.has(l.id));
  const selectedCount = selectedIds.size;

  const submitMutation = useMutation({
    mutationFn: (id: string) => listingApi.submitForReview(id),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      setActionError(getApiErrorMessage(err, "Failed to submit listing for review."));
    },
  });

  const closeMutation = useMutation({
    mutationFn: (id: string) => listingApi.close(id),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      setActionError(getApiErrorMessage(err, "Failed to close listing."));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => listingApi.delete(id),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      setActionError(getApiErrorMessage(err, "Failed to delete listing."));
    },
  });

  const bulkSubmitMutation = useMutation({
    mutationFn: async (listings: ListingSummaryDto[]) => {
      setBulkProgress({ done: 0, total: listings.length });
      let succeeded = 0;
      const failures: BulkSubmitFailure[] = [];

      for (const listing of listings) {
        try {
          await listingApi.submitForReview(listing.id);
          succeeded += 1;
        } catch (error) {
          failures.push({
            id: listing.id,
            title: listing.title,
            message: getApiErrorMessage(error, "Failed to submit listing for review."),
          });
        }
        setBulkProgress((p) =>
          p ? { ...p, done: Math.min(p.done + 1, p.total) } : p,
        );
      }

      return { succeeded, failures } satisfies BulkSubmitOutcome;
    },
    onSuccess: (outcome) => {
      setActionError(null);
      setBulkOutcome(outcome);
      setSelectedIds(new Set());
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onSettled: () => {
      setBulkProgress(null);
    },
  });

  const isMutating =
    submitMutation.isPending ||
    closeMutation.isPending ||
    deleteMutation.isPending ||
    bulkSubmitMutation.isPending;

  const toggleSelected = (id: string, checked: boolean) => {
    setBulkOutcome(null);
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  };

  const toggleSelectAll = (checked: boolean) => {
    setBulkOutcome(null);
    if (!checked) {
      setSelectedIds(new Set());
      return;
    }
    setSelectedIds(new Set(selectableFiltered.map((l) => l.id)));
  };

  const handleBulkSubmit = () => {
    const selected = selectableFiltered.filter((l) => selectedIds.has(l.id));
    if (selected.length === 0) return;
    setBulkOutcome(null);
    setActionError(null);
    bulkSubmitMutation.mutate(selected);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Building2}
        title="My listings"
        description="Manage your properties and publish to the marketplace."
      >
        <HostChannelSyncButton
          onSynced={(msg) => {
            setActionError(null);
            setSyncNote(msg);
          }}
          onError={(msg) => {
            setSyncNote(null);
            setActionError(msg);
          }}
        />
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

      {syncNote && (
        <Alert variant="success">
          <CheckCircle2 className="h-4 w-4" />
          <AlertDescription>{syncNote}</AlertDescription>
        </Alert>
      )}

      {bulkOutcome && (
        <Alert variant={bulkOutcome.failures.length > 0 ? "destructive" : "success"}>
          {bulkOutcome.failures.length === 0 ? (
            <CheckCircle2 className="h-4 w-4" />
          ) : (
            <AlertTriangle className="h-4 w-4" />
          )}
          <AlertDescription>
            <div className="space-y-2">
              <p>
                {bulkOutcome.succeeded > 0
                  ? `${bulkOutcome.succeeded} listing${bulkOutcome.succeeded === 1 ? "" : "s"} submitted for review.`
                  : "No listings were submitted."}
                {bulkOutcome.failures.length > 0
                  ? ` ${bulkOutcome.failures.length} could not be submitted.`
                  : ""}
              </p>
              {bulkOutcome.failures.length > 0 && (
                <ul className="list-disc space-y-1 pl-5 text-sm">
                  {bulkOutcome.failures.map((failure) => (
                    <li key={failure.id}>
                      <span className="font-medium">{failure.title}</span>
                      {" — "}
                      {failure.message}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </AlertDescription>
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
        <div className="space-y-5">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
            <FilterTabs
              aria-label="Filter listings by status"
              options={tabs.map((t) => ({
                value: t.value,
                label: t.label,
                count: counts[t.value] ?? 0,
              }))}
              value={tab}
              onChange={setTab}
              className="lg:flex-1"
            />

            <div className="relative w-full lg:w-72 lg:shrink-0">
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

          {selectableFiltered.length > 0 && (
            <div className="flex flex-col gap-3 rounded-xl border bg-muted/30 p-3 sm:flex-row sm:items-center sm:justify-between">
              <label className="flex items-center gap-2 text-sm">
                <Checkbox
                  checked={allSelectableSelected}
                  onCheckedChange={(checked) => toggleSelectAll(checked)}
                  disabled={isMutating}
                  aria-label="Select all listings that can be submitted for review"
                />
                <span>
                  {selectedCount > 0
                    ? `${selectedCount} selected`
                    : `Select drafts & listings needing changes`}
                  <span className="text-muted-foreground">
                    {" "}
                    ({selectableFiltered.length} eligible)
                  </span>
                </span>
              </label>

              <div className="flex flex-wrap items-center gap-2">
                {selectedCount > 0 && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => setSelectedIds(new Set())}
                    disabled={isMutating}
                  >
                    Clear
                  </Button>
                )}
                <Button
                  type="button"
                  variant="accent"
                  size="sm"
                  onClick={handleBulkSubmit}
                  disabled={selectedCount === 0 || isMutating}
                >
                  {bulkSubmitMutation.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Send className="h-4 w-4" />
                  )}
                  {bulkSubmitMutation.isPending && bulkProgress
                    ? `Submitting ${bulkProgress.done} of ${bulkProgress.total}...`
                    : selectedCount > 0
                      ? `Submit ${selectedCount} for review`
                      : "Submit for review"}
                </Button>
              </div>
            </div>
          )}

          {filtered.length === 0 ? (
            <EmptyState
              title={search ? "No matching listings" : `No ${activeTabLabel.toLowerCase()} listings`}
              description={
                search
                  ? "Try a different search term or clear filters."
                  : tab === "Draft"
                    ? "Drafts will appear here while you finish setting them up."
                    : tab === "InReview"
                      ? "Listings you submit will sit here while an admin reviews them."
                      : tab === "Denied"
                        ? "Listings the admin asked you to fix will appear here."
                        : tab === "Published"
                          ? "Once an admin approves your submission, it will show up here."
                          : tab === "Activated"
                            ? "Activated listings have an active billing subscription."
                            : tab === "Closed"
                              ? "Listings you close will be moved here."
                              : "Create your first listing to appear in search results."
              }
            />
          ) : (
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {filtered.map((l) => (
                <ListingRowCard
                  key={l.id}
                  listing={l}
                  selected={selectedIds.has(l.id)}
                  onSelectedChange={(checked) => toggleSelected(l.id, checked)}
                  onSubmitForReview={() => {
                    setBulkOutcome(null);
                    submitMutation.mutate(l.id);
                  }}
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
        </div>
      )}
    </div>
  );
};

function ListingRowCard({
  listing,
  selected,
  onSelectedChange,
  onSubmitForReview,
  onClose,
  onDelete,
  isMutating,
}: {
  listing: ListingSummaryDto;
  selected: boolean;
  onSelectedChange: (checked: boolean) => void;
  onSubmitForReview: () => void;
  onClose: () => void;
  onDelete: () => void;
  isMutating: boolean;
}) {
  const canSubmit = canSubmitForReview(listing.status);
  const canClose = listing.status === "Published" || listing.status === "Activated";
  const canDelete = listing.status === "Draft" || listing.status === "Denied";
  const submitLabel = listing.status === "Denied" ? "Resubmit" : "Submit for review";

  return (
    <Card
      className={cn(
        "transition-shadow hover:shadow-md",
        selected && "ring-2 ring-primary/40",
      )}
    >
      <div className="relative">
        <Link
          to={`/app/listings/${listing.id}`}
          aria-label={`Open ${listing.title}`}
          className="block aspect-[16/10] overflow-hidden rounded-t-xl bg-muted relative"
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

        {canSubmit && (
          <label
            className="absolute left-3 bottom-3 z-10 flex items-center gap-2 rounded-md bg-background/95 px-2 py-1.5 text-xs font-medium shadow-sm backdrop-blur"
            onClick={(e) => e.stopPropagation()}
          >
            <Checkbox
              checked={selected}
              onCheckedChange={(checked) => onSelectedChange(checked)}
              disabled={isMutating}
              aria-label={`Select ${listing.title}`}
            />
            Select
          </label>
        )}
      </div>

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

        <div className="space-y-2">
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
                className="shrink-0 text-destructive hover:text-destructive"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          </div>
          {canSubmit && (
            <Button
              variant="accent"
              size="sm"
              onClick={onSubmitForReview}
              disabled={isMutating}
              title="Submit to admins for review"
              className="w-full"
            >
              <Send className="h-4 w-4" />
              {submitLabel}
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
