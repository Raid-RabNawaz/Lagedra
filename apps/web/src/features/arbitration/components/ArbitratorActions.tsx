import { useState } from "react";
import { Gavel, XCircle, Loader2, AlertTriangle } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { getApiErrorMessage } from "@/api/errors";
import { useCloseCase, useBeginReview } from "@/features/arbitration/hooks/useArbitration";
import type { CaseDto } from "@/api/types";

type ArbitratorActionsProps = {
  c: CaseDto;
  userId: string;
  onUpdated: () => void;
};

export function ArbitratorActions({ c, userId, onUpdated }: ArbitratorActionsProps) {
  const closeCase = useCloseCase();
  const beginReview = useBeginReview();
  const [error, setError] = useState<string | null>(null);

  const isAssigned = c.assignedArbitratorUserId === userId;
  const canBeginReview = isAssigned && c.status === "EvidenceComplete";
  const canClose = isAssigned && ["Decided", "Appealed"].includes(c.status);
  const waitingForAssignment = !c.assignedArbitratorUserId;

  if (waitingForAssignment) {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Arbitrator</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            You are not assigned to this case yet. Platform operations will assign you when
            evidence is ready.
          </p>
        </CardContent>
      </Card>
    );
  }

  if (!isAssigned) {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Arbitrator</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            Another arbitrator is assigned to this case.
          </p>
        </CardContent>
      </Card>
    );
  }

  if (!canBeginReview && !canClose) {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Arbitrator</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            {c.status === "UnderReview"
              ? "You are reviewing this case. Issue a verdict when ready."
              : `No actions available in status ${c.status}.`}
          </p>
        </CardContent>
      </Card>
    );
  }

  const run = async (fn: () => Promise<unknown>) => {
    setError(null);
    try {
      await fn();
      onUpdated();
    } catch (err) {
      setError(getApiErrorMessage(err, "Action failed."));
    }
  };

  return (
    <Card className="border-blue-200/60">
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Your actions</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {error && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <div className="flex flex-col gap-2">
          {canBeginReview && (
            <Button
              size="sm"
              className="w-full"
              disabled={beginReview.isPending}
              onClick={() => void run(() => beginReview.mutateAsync(c.caseId))}
            >
              {beginReview.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <Gavel className="h-4 w-4 mr-2" />
              )}
              Begin review
            </Button>
          )}
          {canClose && (
            <Button
              size="sm"
              variant="outline"
              className="w-full"
              disabled={closeCase.isPending}
              onClick={() => void run(() => closeCase.mutateAsync(c.caseId))}
            >
              {closeCase.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <XCircle className="h-4 w-4 mr-2" />
              )}
              Close case
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
