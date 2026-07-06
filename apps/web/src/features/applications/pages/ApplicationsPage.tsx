import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  ChevronDown,
  ChevronRight,
  ExternalLink,
  ImageOff,
  Inbox,
  MapPin,
  Plus,
} from "lucide-react";
import { useMyListings } from "@/features/listings/hooks/useMyListings";
import { useMyApplications } from "@/features/applications/hooks/useApplications";
import { ApplicationCard } from "@/features/applications/components/ApplicationCard";
import { ApplicationStatsSummary } from "@/features/applications/components/ApplicationStatsSummary";
import { buttonVariants } from "@/components/ui/button-variants";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { FilterTabs } from "@/components/shared/FilterTabs";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import type {
  DealApplicationDto,
  DealApplicationStatus,
  ListingSummaryDto,
} from "@/api/types";
import { cn } from "@/lib/utils";

type StatusFilter = DealApplicationStatus | "All";

const statusTabs: { value: StatusFilter; label: string }[] = [
  { value: "All", label: "All" },
  { value: "Pending", label: "Pending" },
  { value: "Approved", label: "Approved" },
  { value: "Rejected", label: "Declined" },
  { value: "Expired", label: "Expired" },
  { value: "Cancelled", label: "Cancelled" },
];

type ListingGroup = {
  listing: ListingSummaryDto | null;
  listingId: string;
  listingCity: string | null;
  applications: DealApplicationDto[];
  pendingCount: number;
};

export const ApplicationsPage = () => {
  const {
    data: listings,
    isLoading: listingsLoading,
    isError: listingsError,
    refetch: refetchListings,
  } = useMyListings();
  const {
    data: allApps,
    isLoading: appsLoading,
    isError: appsError,
    refetch: refetchApps,
  } = useMyApplications();
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [collapsedIds, setCollapsedIds] = useState<Set<string>>(new Set());

  const landlordApps = useMemo(() => {
    if (!allApps || !listings) return [];
    const ids = new Set(listings.map((l) => l.id));
    return allApps.filter((a) => ids.has(a.listingId));
  }, [allApps, listings]);

  const counts = useMemo(() => {
    const base: Record<StatusFilter, number> = {
      All: landlordApps.length,
      Pending: 0,
      Approved: 0,
      Rejected: 0,
      Cancelled: 0,
      Expired: 0,
      PaymentFailed: 0,
    };
    for (const a of landlordApps) base[a.status] += 1;
    return base;
  }, [landlordApps]);

  const groups = useMemo<ListingGroup[]>(() => {
    if (!listings) return [];
    const listingMap = new Map<string, ListingSummaryDto>();
    for (const l of listings) listingMap.set(l.id, l);

    const filtered =
      statusFilter === "All"
        ? landlordApps
        : landlordApps.filter((a) => a.status === statusFilter);

    const grouped = new Map<string, DealApplicationDto[]>();
    for (const app of filtered) {
      const existing = grouped.get(app.listingId);
      if (existing) existing.push(app);
      else grouped.set(app.listingId, [app]);
    }

    return Array.from(grouped.entries())
      .map(([listingId, apps]) => {
        const sorted = apps.sort(
          (a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime(),
        );
        return {
          listing: listingMap.get(listingId) ?? null,
          listingId,
          listingCity: sorted[0]?.listingCity ?? null,
          applications: sorted,
          pendingCount: sorted.filter((a) => a.status === "Pending").length,
        };
      })
      .sort(
        (a, b) =>
          b.pendingCount - a.pendingCount ||
          (a.listing?.title ?? "").localeCompare(b.listing?.title ?? ""),
      );
  }, [landlordApps, listings, statusFilter]);

  const toggleCollapsed = (id: string) => {
    setCollapsedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const allCollapsed = groups.length > 0 && groups.every((g) => collapsedIds.has(g.listingId));
  const toggleAll = () => {
    setCollapsedIds(allCollapsed ? new Set() : new Set(groups.map((g) => g.listingId)));
  };

  const isLoading = listingsLoading || appsLoading;
  const isError = listingsError || appsError;

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <PageHeader
        icon={Inbox}
        title="Booking requests"
        description="Review guests who want to stay at your listings. Click a request to see their full profile and stay details."
      >
        {counts.Pending > 0 && (
          <Badge variant="accent" className="h-7 px-3">
            {counts.Pending} need your response
          </Badge>
        )}
      </PageHeader>

      {isLoading ? (
        <ListRowsSkeleton rows={4} />
      ) : isError ? (
        <ErrorState
          title="Couldn't load your inbox"
          message="Something went wrong while loading applications."
          onRetry={() => {
            void refetchListings();
            void refetchApps();
          }}
        />
      ) : !listings || listings.length === 0 ? (
        <EmptyState
          title="No listings yet"
          description="Create a listing first to start receiving applications from guests."
        >
          <Link to="/app/listings/new" className={cn(buttonVariants({ variant: "accent" }))}>
            <Plus className="h-4 w-4" />
            Create listing
          </Link>
        </EmptyState>
      ) : (
        <>
          <ApplicationStatsSummary counts={counts} />

          <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
            <FilterTabs
              aria-label="Filter booking requests by status"
              options={statusTabs.map((t) => ({
                value: t.value,
                label: t.label,
                count: counts[t.value],
              }))}
              value={statusFilter}
              onChange={setStatusFilter}
              hideZeroCounts
              className="lg:flex-1"
            />

            {groups.length > 1 && (
              <Button
                variant="ghost"
                size="sm"
                onClick={toggleAll}
                className="self-start lg:shrink-0"
              >
                {allCollapsed ? "Expand all" : "Collapse all"}
              </Button>
            )}
          </div>

          {groups.length === 0 ? (
            <EmptyState
              title="No applications"
              description={
                statusFilter === "All"
                  ? "No one has applied to your listings yet."
                  : `No ${statusFilter.toLowerCase()} applications.`
              }
            />
          ) : (
            <div className="space-y-4">
              {groups.map((group) => {
                const isCollapsed = collapsedIds.has(group.listingId);
                const hasPending = group.pendingCount > 0;
                return (
                  <Card
                    key={group.listingId}
                    className={cn(
                      "overflow-hidden shadow-sm",
                      hasPending && !isCollapsed && "ring-1 ring-accent/30",
                    )}
                  >
                    <CardHeader
                      className={cn(
                        "p-4",
                        hasPending ? "bg-accent/5" : "bg-muted/30",
                        !isCollapsed && "border-b",
                      )}
                    >
                      <div className="flex items-center justify-between gap-3">
                        <button
                          type="button"
                          onClick={() => toggleCollapsed(group.listingId)}
                          aria-expanded={!isCollapsed}
                          className="flex min-w-0 flex-1 items-center gap-3 text-left"
                        >
                          {isCollapsed ? (
                            <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                          ) : (
                            <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                          )}
                          <span className="relative h-11 w-11 shrink-0 overflow-hidden rounded-lg bg-muted">
                            {group.listing?.coverPhotoUrl ? (
                              <img
                                src={group.listing.coverPhotoUrl}
                                alt=""
                                className="h-full w-full object-cover"
                                loading="lazy"
                              />
                            ) : (
                              <span className="flex h-full w-full items-center justify-center">
                                <ImageOff className="h-4 w-4 text-muted-foreground/40" />
                              </span>
                            )}
                          </span>
                          <span className="min-w-0">
                            <CardTitle className="text-base truncate">
                              {group.listing?.title ?? "Unknown listing"}
                            </CardTitle>
                            <span className="mt-0.5 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                              {group.listingCity && (
                                <span className="flex items-center gap-1">
                                  <MapPin className="h-3 w-3" />
                                  {group.listingCity}
                                </span>
                              )}
                              <span>
                                {group.applications.length}{" "}
                                {group.applications.length === 1 ? "request" : "requests"}
                              </span>
                            </span>
                          </span>
                        </button>
                        <div className="flex shrink-0 items-center gap-2">
                          {group.pendingCount > 0 && (
                            <Badge variant="accent">{group.pendingCount} pending</Badge>
                          )}
                          <Link
                            to={`/listings/${group.listingId}`}
                            onClick={(e) => e.stopPropagation()}
                            aria-label={`View ${group.listing?.title ?? "listing"}`}
                            className="flex items-center gap-1 text-xs text-muted-foreground transition-colors hover:text-foreground"
                          >
                            Listing
                            <ExternalLink className="h-3 w-3" />
                          </Link>
                        </div>
                      </div>
                    </CardHeader>

                    {!isCollapsed && (
                      <CardContent className="space-y-3 bg-muted/10 p-4">
                        {group.applications.map((app) => (
                          <ApplicationCard
                            key={app.applicationId}
                            application={app}
                            perspective="host"
                          />
                        ))}
                      </CardContent>
                    )}
                  </Card>
                );
              })}
            </div>
          )}
        </>
      )}
    </div>
  );
};
