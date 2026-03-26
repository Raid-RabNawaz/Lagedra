import { useMemo } from "react";
import { Link } from "react-router-dom";
import { FileText, ExternalLink } from "lucide-react";
import { useMyApplications } from "@/features/applications/hooks/useApplications";
import { ApplicationCard } from "@/features/applications/components/ApplicationCard";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import type { DealApplicationDto } from "@/api/types";

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
  const { data, isLoading, isError } = useMyApplications();

  const groups = useMemo(() => (data ? groupByListing(data) : []), [data]);

  const totalPending = groups.reduce((sum, g) => sum + g.pendingCount, 0);

  return (
    <div className="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex items-center gap-3 mb-6">
        <FileText className="h-6 w-6" />
        <h1 className="text-2xl font-bold tracking-tight">My Applications</h1>
        {totalPending > 0 && (
          <Badge variant="secondary">{totalPending} pending</Badge>
        )}
      </div>

      {isLoading ? (
        <Loader label="Loading your applications..." />
      ) : isError ? (
        <div className="py-12 text-center">
          <p className="text-destructive font-medium">Failed to load applications.</p>
        </div>
      ) : groups.length === 0 ? (
        <EmptyState
          title="No applications yet"
          description="When you apply for a listing, your applications will appear here grouped by property."
        />
      ) : (
        <div className="space-y-6">
          {groups.map((group) => (
            <Card key={group.listingId}>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between gap-3">
                  <div className="flex items-center gap-2 min-w-0">
                    <CardTitle className="text-base truncate">
                      Listing
                    </CardTitle>
                    {group.pendingCount > 0 && (
                      <Badge variant="secondary" className="shrink-0">
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
    </div>
  );
};
