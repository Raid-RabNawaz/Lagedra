import { useEffect, useState } from "react";
import { Zap, UserCheck, Loader2, Users, AlertTriangle } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { adminApi } from "@/features/admin/services/adminApi";
import { getApiErrorMessage } from "@/api/errors";
import {
  useAssignArbitrator,
  useMarkEvidenceComplete,
  useBeginReview,
} from "@/features/arbitration/hooks/useArbitration";
import type { ArbitratorCaseloadDto, CaseDto } from "@/api/types";

type CaseAssignmentPanelProps = {
  c: CaseDto;
  onUpdated: () => void;
};

export function CaseAssignmentPanel({ c, onUpdated }: CaseAssignmentPanelProps) {
  const assignArbitrator = useAssignArbitrator();
  const markEvidence = useMarkEvidenceComplete();
  const beginReview = useBeginReview();

  const [panel, setPanel] = useState<ArbitratorCaseloadDto[]>([]);
  const [arbitratorId, setArbitratorId] = useState("");
  const [autoAssigning, setAutoAssigning] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  useEffect(() => {
    void adminApi.getArbitratorCaseload().then(setPanel).catch(() => setPanel([]));
  }, [c.assignedArbitratorUserId]);

  const canMarkEvidence = c.status === "EvidencePending";
  const canAssign =
    !c.assignedArbitratorUserId
    && !["Decided", "Closed", "Appealed"].includes(c.status);
  const canBeginReview =
    c.status === "EvidenceComplete" && Boolean(c.assignedArbitratorUserId);
  const panelEmpty = panel.length === 0;
  const allAtCap = panel.length > 0 && panel.every((a) => a.isAtHardCap);

  const runAction = async (label: string, fn: () => Promise<void>) => {
    setActionError(null);
    setActionSuccess(null);
    try {
      await fn();
      setActionSuccess(label);
      onUpdated();
    } catch (err) {
      setActionError(getApiErrorMessage(err, "Action failed."));
    }
  };

  const handleAutoAssign = async () => {
    setAutoAssigning(true);
    await runAction("Arbitrator auto-assigned", async () => {
      const { arbitratorUserId } = await adminApi.autoAssignArbitrator(c.caseId);
      setArbitratorId(arbitratorUserId);
    });
    setAutoAssigning(false);
  };

  if (c.assignedArbitratorUserId && !canAssign && !canMarkEvidence && !canBeginReview) {
    return (
      <Card className="border-emerald-200/60 bg-emerald-50/30">
        <CardHeader className="pb-2">
          <CardTitle className="text-base flex items-center gap-2">
            <UserCheck className="h-4 w-4 text-emerald-700" />
            Arbitrator assigned
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm font-medium">
            {c.assignedArbitratorEmail ?? "Assigned arbitrator"}
          </p>
          <p className="text-xs text-muted-foreground mt-1">
            Status: {c.status.replace(/([A-Z])/g, " $1").trim()}
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="border-violet-200/50">
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <Users className="h-4 w-4 text-violet-600" />
          Platform operations
        </CardTitle>
        <CardDescription>
          Assign an arbitrator when evidence is ready. The arbitrator starts review when they are
          ready — assignment does not skip that step.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {actionError && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>{actionError}</AlertDescription>
          </Alert>
        )}
        {actionSuccess && (
          <Alert className="border-emerald-200 bg-emerald-50 text-emerald-900">
            <AlertDescription>{actionSuccess}</AlertDescription>
          </Alert>
        )}

        {panelEmpty && (
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
              No arbitrators on the panel. Create a user with role <strong>Arbitrator</strong> in
              admin users (e.g. arbitrator@lagedra.dev).
            </AlertDescription>
          </Alert>
        )}

        {allAtCap && (
          <Alert>
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
              All panel arbitrators are at the hard cap (20 active cases). Manual assignment may
              still fail until caseload frees up.
            </AlertDescription>
          </Alert>
        )}

        <div className="flex flex-wrap gap-2">
          {canMarkEvidence && (
            <Button
              size="sm"
              variant="outline"
              disabled={markEvidence.isPending}
              onClick={() =>
                void runAction("Evidence marked complete", () =>
                  markEvidence.mutateAsync(c.caseId).then(() => undefined),
                )
              }
            >
              {markEvidence.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : null}
              Mark evidence complete
            </Button>
          )}
          {canBeginReview && (
            <Button
              size="sm"
              variant="outline"
              disabled={beginReview.isPending}
              onClick={() =>
                void runAction("Review started for arbitrator", () =>
                  beginReview.mutateAsync(c.caseId).then(() => undefined),
                )
              }
            >
              {beginReview.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : null}
              Begin review (on behalf of arbitrator)
            </Button>
          )}
        </div>

        {canAssign && (
          <div className="rounded-lg border bg-muted/30 p-4 space-y-3">
            <div className="flex items-center justify-between gap-2">
              <p className="text-sm font-medium">Assign arbitrator</p>
              {c.status !== "EvidenceComplete" && (
                <Badge variant="accent">Best after Review Ready</Badge>
              )}
            </div>

            {panel.length > 0 && (
              <div className="grid gap-2 sm:grid-cols-2 max-h-40 overflow-y-auto">
                {panel.map((a) => (
                  <div
                    key={a.arbitratorUserId}
                    className="text-xs rounded-md border bg-background px-2 py-1.5 flex justify-between gap-2"
                  >
                    <span className="truncate">{a.displayName ?? a.email}</span>
                    <span className="text-muted-foreground shrink-0">
                      {a.activeCaseCount}
                      {a.isAtHardCap ? " · cap" : ""}
                    </span>
                  </div>
                ))}
              </div>
            )}

            <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
              <select
                className="flex-1 rounded-lg border bg-background px-3 py-2 text-sm min-h-10"
                value={arbitratorId}
                onChange={(e) => setArbitratorId(e.target.value)}
                disabled={panelEmpty}
              >
                <option value="">Select arbitrator…</option>
                {panel.map((a) => (
                  <option
                    key={a.arbitratorUserId}
                    value={a.arbitratorUserId}
                    disabled={a.isAtHardCap}
                  >
                    {a.displayName ?? a.email} ({a.activeCaseCount} active
                    {a.isAtHardCap ? ", at cap" : ""})
                  </option>
                ))}
              </select>
              <Button
                size="sm"
                variant="default"
                className="shrink-0"
                disabled={autoAssigning || panelEmpty || allAtCap}
                onClick={() => void handleAutoAssign()}
              >
                {autoAssigning ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <Zap className="h-4 w-4 mr-2" />
                )}
                Auto-assign
              </Button>
              <Button
                size="sm"
                variant="outline"
                className="shrink-0"
                disabled={!arbitratorId || assignArbitrator.isPending}
                onClick={() =>
                  void runAction("Arbitrator assigned", () =>
                    assignArbitrator
                      .mutateAsync({
                        caseId: c.caseId,
                        arbitratorUserId: arbitratorId,
                      })
                      .then(() => undefined),
                  )
                }
              >
                {assignArbitrator.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <UserCheck className="h-4 w-4 mr-2" />
                )}
                Assign
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
