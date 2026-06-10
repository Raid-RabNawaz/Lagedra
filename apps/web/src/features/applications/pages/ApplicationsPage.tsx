import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  ChevronDown,
  ChevronRight,
  ExternalLink,
  ImageOff,
  Inbox,
  Plus,
} from "lucide-react";
import { useMyListings } from "@/features/listings/hooks/useMyListings";
import { useMyApplications } from "@/features/applications/hooks/useApplications";
import { ApplicationCard } from "@/features/applications/components/ApplicationCard";
import { buttonVariants } from "@/components/ui/button-variants";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
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
  { value: "Rejected", label: "Rejected" },
  { value: "Cancelled", label: "Cancelled" },
];

type ListingGroup = {
  listing: ListingSummaryDto | null;
  listingId: string;
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

  // Applications on listings the current user owns (the "inbox" view).
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
      .map(([listingId, apps]) => ({
        listing: listingMap.get(listingId) ?? null,
        listingId,
        applications: apps.sort(
          (a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime(),
        ),
        pendingCount: apps.filter((a) => a.status === "Pending").length,
      }))
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
    <div className="mx-auto max-w-4xl space-y-6">
      <PageHeader
        icon={Inbox}
        title="Booking requests"
        description="Guests who applied to stay at your listings, grouped by property."
      >
        {counts.Pending > 0 && (
          <Badge variant="accent" className="h-7 px-3">
            {counts.Pending} pending
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
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <Tabs value={statusFilter} onValueChange={(v) => setStatusFilter(v as StatusFilter)}>
              <TabsList className="overflow-x-auto">
                {statusTabs.map((t) => (
                  <TabsTrigger key={t.value} value={t.value} className="gap-1.5">
                    {t.label}
                    <span
                      className={cn(
                        "rounded-full px-1.5 text-[10px] font-semibold tabular-nums",
                        statusFilter === t.value
                          ? "bg-foreground text-background"
                          : "bg-muted text-muted-foreground",
                      )}
                    >
                      {counts[t.value]}
                    </span>
                  </TabsTrigger>
                ))}
              </TabsList>
            </Tabs>

            {groups.length > 1 && (
              <Button variant="ghost" size="sm" onClick={toggleAll}>
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
                return (
                  <Card key={group.listingId}>
                    <CardHeader className="pb-3">
                      <button
                        onClick={() => toggleCollapsed(group.listingId)}
                        className="flex items-center justify-between gap-3 w-full text-left cursor-pointer"
                      >
                        <div className="flex items-center gap-3 min-w-0">
                          {isCollapsed ? (
                            <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                          ) : (
                            <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                          )}
                          <span className="relative hidden h-10 w-10 shrink-0 overflow-hidden rounded-lg bg-muted sm:block">
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
                          <CardTitle className="text-base truncate">
                            {group.listing?.title ?? "Unknown listing"}
                          </CardTitle>
                          <Badge variant="secondary" className="shrink-0">
                            {group.applications.length}
                          </Badge>
                          {group.pendingCount > 0 && (
                            <Badge variant="accent" className="shrink-0">
                              {group.pendingCount} pending
                            </Badge>
                          )}
                        </div>
                        <Link
                          to={`/listings/${group.listingId}`}
                          onClick={(e) => e.stopPropagation()}
                          className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors shrink-0"
                        >
                          View
                          <ExternalLink className="h-3 w-3" />
                        </Link>
                      </button>
                    </CardHeader>

                    {!isCollapsed && (
                      <CardContent className="space-y-3 pt-0">
                        {group.applications.map((app) => (
                          <ApplicationCard
                            key={app.applicationId}
                            application={app}
                            showHostActions
                            defaultDepositCents={group.listing?.defaultDepositCents ?? null}
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
