import { useParams, Navigate, Link } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { useSnapshotByDealId } from "@/features/truth-surface/hooks/useTruthSurface";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";

export function DealTruthSurfacePage() {
  const { dealId } = useParams<{ dealId: string }>();
  const { data: snapshot, isLoading, isError } = useSnapshotByDealId(dealId);

  if (isLoading) {
    return <Loader label="Loading truth surface..." />;
  }

  if (snapshot) {
    return <Navigate to={`/app/truth-surface/${snapshot.snapshotId}`} replace />;
  }

  return (
    <EmptyState
      title={isError ? "Failed to load" : "No truth surface yet"}
      description={
        isError
          ? "Could not load the truth surface. Please try again."
          : "A truth surface snapshot has not been created for this deal yet. It will be created once the inquiry phase is complete."
      }
    >
      <Link to={dealId ? `/app/deals/${dealId}` : "/app/deals"}>
        <Button variant="outline" size="sm">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to deal
        </Button>
      </Link>
    </EmptyState>
  );
}
