import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Inbox, ExternalLink, ChevronDown, ChevronRight } from "lucide-react";
import { useMyListings } from "@/features/listings/hooks/useMyListings";
import { useMyApplications } from "@/features/applications/hooks/useApplications";
import { ApplicationCard } from "@/features/applications/components/ApplicationCard";
import { Select } from "@/components/ui/select";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { cn } from "@/lib/utils";
import type { DealApplicationDto, DealApplicationStatus, ListingSummaryDto } from "@/api/types";

const statusFilters: { value: DealApplicationStatus | ""; label: string }[] = [
  { value: "", label: "All statuses" },
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
  const { data: listings, isLoading: listingsLoading } = useMyListings();
  const { data: allApps, isLoading: appsLoading, isError } = useMyApplications();
  const [statusFilter, setStatusFilter] = useState<DealApplicationStatus | "">("");
  const [collapsedIds, setCollapsedIds] = useState<Set<string>>(new Set());

  const groups = useMemo<ListingGroup[]>(() => {
    if (!allApps || !listings) return [];

    const listingMap = new Map<string, ListingSummaryDto>();
    for (const l of listings) listingMap.set(l.id, l);

    const landlordApps = allApps.filter((a) =>
      listingMap.has(a.listingId),
    );

    const filtered = statusFilter
      ? landlordApps.filter((a) => a.status === statusFilter)
      : landlordApps;

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
      .sort((a, b) => b.pendingCount - a.pendingCount || (a.listing?.title ?? "").localeCompare(b.listing?.title ?? ""));
  }, [allApps, listings, statusFilter]);

  const totalPending = groups.reduce((sum, g) => sum + g.pendingCount, 0);
  const totalApps = groups.reduce((sum, g) => sum + g.applications.length, 0);

  const toggleCollapsed = (id: string) => {
    setCollapsedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const isLoading = listingsLoading || appsLoading;

  if (isLoading) return <Loader fullPage label="Loading applications..." />;

  if (!listings || listings.length === 0) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
        <EmptyState
          title="No listings yet"
          description="Create a listing first to start receiving applications."
        />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex items-center gap-3 mb-6">
        <Inbox className="h-6 w-6" />
        <h1 className="text-2xl font-bold tracking-tight">Applications</h1>
        {totalPending > 0 && (
          <Badge variant="accent">{totalPending} pending</Badge>
        )}
        {totalApps > 0 && (
          <span className="text-sm text-muted-foreground">
            {totalApps} total across {groups.length} listing{groups.length !== 1 ? "s" : ""}
          </span>
        )}
      </div>

      <div className="flex items-end gap-3 mb-6">
        <div className="space-y-1.5 w-48">
          <label className="text-xs font-medium text-muted-foreground">Filter by status</label>
          <Select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as DealApplicationStatus | "")}
            className="h-10"
          >
            {statusFilters.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </Select>
        </div>
      </div>

      {isError ? (
        <div className="py-12 text-center">
          <p className="text-destructive font-medium">Failed to load applications.</p>
        </div>
      ) : groups.length === 0 ? (
        <EmptyState
          title="No applications"
          description={
            statusFilter
              ? `No ${statusFilter.toLowerCase()} applications found.`
              : "No one has applied to your listings yet."
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
                    <div className="flex items-center gap-2 min-w-0">
                      {isCollapsed ? (
                        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                      ) : (
                        <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                      )}
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
                      <ApplicationCard key={app.applicationId} application={app} />
                    ))}
                  </CardContent>
                )}
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
};
