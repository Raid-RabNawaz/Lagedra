import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Building2, ExternalLink, FileText, Search } from "lucide-react";
import { useMyApplications } from "@/features/applications/hooks/useApplications";
import { ApplicationCard } from "@/features/applications/components/ApplicationCard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { EmptyState } from "@/components/shared/EmptyState";
import { ErrorState } from "@/components/shared/ErrorState";
import type { DealApplicationDto, DealApplicationStatus } from "@/api/types";
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
  listingId: string;
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
    .map(([listingId, apps]) => ({
      listingId,
      applications: apps.sort(
        (a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime(),
      ),
      pendingCount: apps.filter((a) => a.status === "Pending").length,
      latestSubmission: apps.reduce(
        (latest, a) => (a.submittedAt > latest ? a.submittedAt : latest),
        apps[0]!.submittedAt,
      ),
    }))
    .sort((a, b) => new Date(b.latestSubmission).getTime() - new Date(a.latestSubmission).getTime());
}

export const MyApplicationsPage = () => {
  const { data, isLoading, isError, refetch } = useMyApplications();
  const [status, setStatus] = useState<StatusFilter>("All");

  const counts = useMemo(() => {
    const base: Record<StatusFilter, number> = {
      All: data?.length ?? 0,
      Pending: 0,
      Approved: 0,
      Rejected: 0,
      Cancelled: 0,
    };
    for (const a of data ?? []) base[a.status] += 1;
    return base;
  }, [data]);

  const groups = useMemo(() => {
    if (!data) return [];
    const filtered = status === "All" ? data : data.filter((a) => a.status === status);
    return groupByListing(filtered);
  }, [data, status]);

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <PageHeader
        icon={FileText}
        title="My applications"
        description="Track the listings you've applied for, grouped by property."
      />

      {isLoading ? (
        <ListRowsSkeleton rows={4} />
      ) : isError ? (
        <ErrorState
          title="Couldn't load applications"
          message="Something went wrong while loading your applications."
          onRetry={() => void refetch()}
        />
      ) : (data?.length ?? 0) === 0 ? (
        <EmptyState
          title="No applications yet"
          description="When you apply for a listing, your applications will appear here grouped by property."
        >
          <Link to="/listings">
            <Button variant="accent" size="sm">
              <Search className="h-4 w-4" />
              Browse listings
            </Button>
          </Link>
        </EmptyState>
      ) : (
        <>
          <Tabs value={status} onValueChange={(v) => setStatus(v as StatusFilter)}>
            <TabsList className="overflow-x-auto">
              {statusTabs.map((t) => (
                <TabsTrigger key={t.value} value={t.value} className="gap-1.5">
                  {t.label}
                  <span
                    className={cn(
                      "rounded-full px-1.5 text-[10px] font-semibold tabular-nums",
                      status === t.value
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

          {groups.length === 0 ? (
            <EmptyState
              title="No matching applications"
              description={`You have no ${status.toLowerCase()} applications.`}
            />
          ) : (
            <div className="space-y-6">
              {groups.map((group) => (
                <Card key={group.listingId}>
                  <CardHeader className="pb-3">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex items-center gap-2 min-w-0">
                        <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                          <Building2 className="h-4 w-4" />
                        </span>
                        <CardTitle className="text-base truncate">Property</CardTitle>
                        <Badge variant="secondary" className="shrink-0">
                          {group.applications.length}
                          {group.applications.length === 1 ? " application" : " applications"}
                        </Badge>
                        {group.pendingCount > 0 && (
                          <Badge variant="accent" className="shrink-0">
                            {group.pendingCount} pending
                          </Badge>
                        )}
                      </div>
                      <Link
                        to={`/listings/${group.listingId}`}
                        className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors shrink-0"
                      >
                        View listing
                        <ExternalLink className="h-3 w-3" />
                      </Link>
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    {group.applications.map((app) => (
                      <ApplicationCard key={app.applicationId} application={app} />
                    ))}
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
};
