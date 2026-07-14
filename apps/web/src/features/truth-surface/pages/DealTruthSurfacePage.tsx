import { useParams, Navigate } from "react-router-dom";
import { useSnapshotByDealId } from "@/features/truth-surface/hooks/useTruthSurface";
import { BackLink } from "@/components/shared/BackLink";
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
      <BackLink
        fallbackTo={dealId ? `/app/deals/${dealId}` : "/app/deals"}
        variant="button"
        label="Back to deal"
      />
    </EmptyState>
  );
}
