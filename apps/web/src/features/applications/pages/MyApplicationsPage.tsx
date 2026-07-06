import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  ChevronDown,
  ChevronRight,
  ExternalLink,
  FileText,
  ImageOff,
  MapPin,
  Search,
} from "lucide-react";
import { useMyApplications } from "@/features/applications/hooks/useApplications";
import { useAuthStore } from "@/app/auth/authStore";
import { ApplicationCard } from "@/features/applications/components/ApplicationCard";
import { ApplicationStatsSummary } from "@/features/applications/components/ApplicationStatsSummary";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { FilterTabs } from "@/components/shared/FilterTabs";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import { cn } from "@/lib/utils";
import type { DealApplicationDto, DealApplicationStatus } from "@/api/types";

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
  listingId: string;
  listingTitle: string;
  listingCoverPhotoUri: string | null;
  listingCity: string | null;
  applications: DealApplicationDto[];
  pendingCount: number;
  latestSubmission: string;
};

function groupByListing(applications: DealApplicationDto[]): ListingGroup[] {
  const map = new Map<string, DealApplicationDto[]>();

  for (const app of applications) {
    const existing = map.get(app.listingId);
    if (existing) {
      existing.push(app);
    } else {
      map.set(app.listingId, [app]);
    }
  }

  return Array.from(map.entries())
    .map(([listingId, apps]) => {
      const sorted = apps.sort(
        (a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime(),
      );
      const head = sorted[0]!;
      return {
        listingId,
        listingTitle: head.listingTitle ?? "Listing",
        listingCoverPhotoUri: head.listingCoverPhotoUri ?? null,
        listingCity: head.listingCity ?? null,
        applications: sorted,
        pendingCount: sorted.filter((a) => a.status === "Pending").length,
        latestSubmission: sorted.reduce(
          (latest, a) => (a.submittedAt > latest ? a.submittedAt : latest),
          head.submittedAt,
        ),
      };
    })
    .sort((a, b) => new Date(b.latestSubmission).getTime() - new Date(a.latestSubmission).getTime());
}

export const MyApplicationsPage = () => {
  const { data, isLoading, isError, refetch } = useMyApplications();
  const user = useAuthStore((s) => s.user);
  const [status, setStatus] = useState<StatusFilter>("All");
  const [collapsedIds, setCollapsedIds] = useState<Set<string>>(new Set());

  const myApps = useMemo(
    () => (data ?? []).filter((a) => !user || a.tenantUserId === user.userId),
    [data, user],
  );

  const counts = useMemo(() => {
    const base: Record<StatusFilter, number> = {
      All: myApps.length,
      Pending: 0,
      Approved: 0,
      Rejected: 0,
      Cancelled: 0,
      Expired: 0,
      PaymentFailed: 0,
    };
    for (const a of myApps) base[a.status] += 1;
    return base;
  }, [myApps]);

  const groups = useMemo(() => {
    const filtered = status === "All" ? myApps : myApps.filter((a) => a.status === status);
    return groupByListing(filtered);
  }, [myApps, status]);

  const toggleCollapsed = (id: string) => {
    setCollapsedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <PageHeader
        icon={FileText}
        title="My applications"
        description="Track your booking requests. Click any request to see full stay details and your host's profile."
      >
        {counts.Pending > 0 && (
          <Badge variant="accent" className="h-7 px-3">
            {counts.Pending} awaiting response
          </Badge>
        )}
      </PageHeader>

      {isLoading ? (
        <ListRowsSkeleton rows={4} />
      ) : isError ? (
        <ErrorState
          title="Couldn't load applications"
          message="Something went wrong while loading your applications."
          onRetry={() => void refetch()}
        />
      ) : myApps.length === 0 ? (
        <EmptyState
          title="No applications yet"
          description="When you request to book a listing, it will appear here grouped by property."
        >
          <Link to="/listings">
            <Button variant="accent" size="sm">
              <Search className="h-4 w-4" />
              Browse listings
            </Button>
          </Link>
        </EmptyState>
      ) : (
        <div className="space-y-5">
          <ApplicationStatsSummary counts={counts} />

          <FilterTabs
            aria-label="Filter applications by status"
            options={statusTabs.map((t) => ({
              value: t.value,
              label: t.label,
              count: counts[t.value],
            }))}
            value={status}
            onChange={setStatus}
            hideZeroCounts
          />

          {groups.length === 0 ? (
            <EmptyState
              title="No matching applications"
              description={`You have no ${status.toLowerCase()} applications.`}
            />
          ) : (
            <div className="space-y-4">
              {groups.map((group) => {
                const isCollapsed = collapsedIds.has(group.listingId);
                return (
                  <Card key={group.listingId} className="overflow-hidden shadow-sm">
                    <CardHeader
                      className={cn(
                        "bg-muted/30 p-4",
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
                          <span className="relative h-12 w-12 shrink-0 overflow-hidden rounded-lg bg-muted">
                            {group.listingCoverPhotoUri ? (
                              <img
                                src={group.listingCoverPhotoUri}
                                alt=""
                                className="h-full w-full object-cover"
                                loading="lazy"
                              />
                            ) : (
                              <span className="flex h-full w-full items-center justify-center">
                                <ImageOff className="h-5 w-5 text-muted-foreground/40" />
                              </span>
                            )}
                          </span>
                          <span className="min-w-0">
                            <span className="block truncate font-semibold leading-tight">
                              {group.listingTitle}
                            </span>
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
                            aria-label={`View ${group.listingTitle}`}
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
                            perspective="tenant"
                          />
                        ))}
                      </CardContent>
                    )}
                  </Card>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
