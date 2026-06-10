import { Link } from "react-router-dom";
import { Fingerprint, ExternalLink } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { useSnapshotByDealId } from "@/features/truth-surface/hooks/useTruthSurface";
import { TruthSnapshotViewer } from "@/features/truth-surface/components/TruthSnapshotViewer";

type CaseTruthSurfaceSectionProps = {
  dealId: string;
};

export function CaseTruthSurfaceSection({ dealId }: CaseTruthSurfaceSectionProps) {
  const { data: snapshot, isLoading, isError } = useSnapshotByDealId(dealId);

  if (isLoading) {
    return <Loader label="Loading truth surface..." />;
  }

  if (isError || !snapshot) {
    return (
      <EmptyState
        title="No truth surface"
        description="This deal does not have a confirmed truth surface snapshot yet. The arbitrator should verify deal terms with platform support if one is expected."
      >
        <Fingerprint className="h-10 w-10 text-muted-foreground/40" />
      </EmptyState>
    );
  }

  return (
    <div className="space-y-3">
      <TruthSnapshotViewer snapshot={snapshot} />
      <Link to={`/app/truth-surface/${snapshot.snapshotId}`}>
        <Button variant="outline" size="sm" className="gap-2">
          <ExternalLink className="h-4 w-4" />
          Open full truth surface
        </Button>
      </Link>
    </div>
  );
}
