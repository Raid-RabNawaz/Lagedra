import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  Gavel,
  DollarSign,
  CheckCircle2,
  AlertTriangle,
  Send,
  XCircle,
  Loader2,
  UserCheck,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { formatDate, formatMoney } from "@/utils/format";
import { useAuthStore } from "@/app/auth/authStore";
import { isAdmin } from "@/app/auth/permissions";
import { CaseTimeline } from "@/features/arbitration/components/CaseTimeline";
import { EvidenceUpload } from "@/features/arbitration/components/EvidenceUpload";
import {
  useCase,
  useMarkEvidenceComplete,
  useIssueDecision,
  useCloseCase,
  useAppealCase,
  useAssignArbitrator,
} from "@/features/arbitration/hooks/useArbitration";
import type { ArbitrationTier, CaseDto } from "@/api/types";

function tierLabel(tier: ArbitrationTier) {
  return tier === "BindingArbitration" ? "Binding Arbitration" : "Protocol Adjudication";
}

function categoryLabel(category: string) {
  const labels: Record<string, string> = {
    CategoryA: "Insurance Lapse",
    CategoryB: "Payment Default",
    CategoryC: "Lease Violation",
    CategoryD: "Property Damage",
    CategoryE: "Unauthorized Occupants",
    CategoryF: "Early Termination",
    CategoryG: "Rule Violation",
    Other: "Other",
  };
  return labels[category] ?? category;
}

function CaseHeader({ c }: { c: CaseDto }) {
  return (
    <Card>
      <CardContent className="p-5 space-y-4">
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
              <Gavel className="h-5 w-5" />
            </div>
            <div>
              <h2 className="font-semibold text-lg">{categoryLabel(c.category)}</h2>
              <p className="text-sm text-muted-foreground">
                {tierLabel(c.tier)}
              </p>
            </div>
          </div>
          <Badge
            variant={
              c.status === "Decided" || c.status === "Closed"
                ? "success"
                : c.status === "Appealed"
                  ? "destructive"
                  : "secondary"
            }
          >
            {c.status}
          </Badge>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
          <div>
            <p className="text-muted-foreground text-xs">Filed</p>
            <p className="font-medium">{formatDate(c.filedAt)}</p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Filing Fee</p>
            <p className="font-medium">{formatMoney(c.filingFeeCents)}</p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Evidence Slots</p>
            <p className="font-medium">{c.evidenceSlotCount}</p>
          </div>
          {c.decisionDueAt && (
            <div>
              <p className="text-muted-foreground text-xs">Decision Due</p>
              <p className="font-medium">{formatDate(c.decisionDueAt)}</p>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function DecisionSection({ c, isAdminOrArbitrator }: { c: CaseDto; isAdminOrArbitrator: boolean }) {
  const issueDecision = useIssueDecision();
  const [summary, setSummary] = useState("");
  const [awardAmount, setAwardAmount] = useState("");

  if (c.decision) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 text-green-600" />
            Decision
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-sm">{c.decision.summary}</p>
          {c.decision.awardAmount != null && (
            <div className="flex items-center gap-2 text-sm">
              <DollarSign className="h-4 w-4 text-muted-foreground" />
              <span className="font-medium">
                Award: ${c.decision.awardAmount.toLocaleString()}
              </span>
            </div>
          )}
          <p className="text-xs text-muted-foreground">
            Decided {formatDate(c.decision.decidedAt)}
          </p>
        </CardContent>
      </Card>
    );
  }

  if (!isAdminOrArbitrator || !["UnderReview", "EvidenceComplete"].includes(c.status)) {
    return null;
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Issue Decision</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <textarea
          className="w-full rounded-lg border bg-background px-3 py-2 text-sm min-h-[80px] resize-y"
          placeholder="Decision summary..."
          value={summary}
          onChange={(e) => setSummary(e.target.value)}
        />
        {c.tier === "BindingArbitration" && (
          <input
            type="number"
            className="w-full rounded-lg border bg-background px-3 py-2 text-sm"
            placeholder="Award amount (optional)"
            value={awardAmount}
            onChange={(e) => setAwardAmount(e.target.value)}
          />
        )}
        <Button
          size="sm"
          disabled={!summary.trim() || issueDecision.isPending}
          onClick={() =>
            issueDecision.mutate({
              caseId: c.caseId,
              decisionSummary: summary.trim(),
              awardAmount: awardAmount ? Number(awardAmount) : null,
            })
          }
        >
          {issueDecision.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin mr-2" />
          ) : (
            <Send className="h-4 w-4 mr-2" />
          )}
          Issue Decision
        </Button>
      </CardContent>
    </Card>
  );
}

function AppealSection({ c, userId }: { c: CaseDto; userId: string }) {
  const appealCase = useAppealCase();
  const [reason, setReason] = useState("");

  if (c.status !== "Decided" || c.filedByUserId !== userId) {
    return null;
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <AlertTriangle className="h-4 w-4 text-amber-500" />
          Appeal Decision
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <textarea
          className="w-full rounded-lg border bg-background px-3 py-2 text-sm min-h-[60px] resize-y"
          placeholder="Reason for appeal..."
          value={reason}
          onChange={(e) => setReason(e.target.value)}
        />
        <Button
          size="sm"
          variant="destructive"
          disabled={!reason.trim() || appealCase.isPending}
          onClick={() =>
            appealCase.mutate({ caseId: c.caseId, reason: reason.trim() })
          }
        >
          {appealCase.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin mr-2" />
          ) : (
            <AlertTriangle className="h-4 w-4 mr-2" />
          )}
          File Appeal
        </Button>
      </CardContent>
    </Card>
  );
}

function AdminActions({ c, isAdminOrArbitrator }: { c: CaseDto; isAdminOrArbitrator: boolean }) {
  const closeCase = useCloseCase();
  const markEvidence = useMarkEvidenceComplete();
  const assignArbitrator = useAssignArbitrator();
  const [arbitratorId, setArbitratorId] = useState("");

  if (!isAdminOrArbitrator) return null;

  const canClose = ["Decided", "Appealed"].includes(c.status);
  const canMarkEvidence = c.status === "EvidencePending";
  const canAssign = ["EvidenceComplete", "Filed", "EvidencePending"].includes(c.status);

  if (!canClose && !canMarkEvidence && !canAssign) return null;

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <UserCheck className="h-4 w-4" />
          Admin Actions
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex gap-2 flex-wrap">
          {canMarkEvidence && (
            <Button
              size="sm"
              variant="outline"
              disabled={markEvidence.isPending}
              onClick={() => markEvidence.mutate(c.caseId)}
            >
              {markEvidence.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <CheckCircle2 className="h-4 w-4 mr-2" />
              )}
              Mark Evidence Complete
            </Button>
          )}
          {canClose && (
            <Button
              size="sm"
              variant="outline"
              disabled={closeCase.isPending}
              onClick={() => closeCase.mutate(c.caseId)}
            >
              {closeCase.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <XCircle className="h-4 w-4 mr-2" />
              )}
              Close Case
            </Button>
          )}
        </div>
        {canAssign && (
          <div className="flex gap-2 items-end">
            <input
              type="text"
              className="flex-1 rounded-lg border bg-background px-3 py-2 text-sm"
              placeholder="Arbitrator User ID"
              value={arbitratorId}
              onChange={(e) => setArbitratorId(e.target.value)}
            />
            <Button
              size="sm"
              disabled={!arbitratorId.trim() || assignArbitrator.isPending}
              onClick={() =>
                assignArbitrator.mutate({
                  caseId: c.caseId,
                  arbitratorUserId: arbitratorId.trim(),
                  concurrentCaseCount: 1,
                })
              }
            >
              {assignArbitrator.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <UserCheck className="h-4 w-4 mr-2" />
              )}
              Assign Arbitrator
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export function CaseDetailPage() {
  const { caseId } = useParams<{ caseId: string }>();
  const user = useAuthStore((s) => s.user);
  const isAdminOrArbitrator =
    isAdmin(user?.role ?? "") || String(user?.role) === "Arbitrator";
  const userId = user?.userId ?? "";

  const { data: c, isLoading, error } = useCase(caseId);
  const [localManifestId, setLocalManifestId] = useState<string | undefined>(
    undefined,
  );

  if (isLoading) {
    return <Loader label="Loading case..." />;
  }

  if (error || !c) {
    return (
      <EmptyState
        title="Case not found"
        description="This case may not exist or you may not have access."
      >
        <Link to="/app/arbitration">
          <Button variant="outline" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to cases
          </Button>
        </Link>
      </EmptyState>
    );
  }

  const existingManifestId = c.evidenceSlots?.[0]?.evidenceManifestId;
  const manifestId = localManifestId ?? existingManifestId;

  const showEvidence = [
    "Filed",
    "EvidencePending",
    "EvidenceComplete",
    "UnderReview",
    "Decided",
    "Closed",
    "Appealed",
  ].includes(c.status);
  const evidenceReadOnly = !["Filed", "EvidencePending", "Appealed"].includes(c.status);

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link
        to="/app/arbitration"
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ArrowLeft className="h-4 w-4" />
        All Cases
      </Link>

      <CaseHeader c={c} />

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Progress</CardTitle>
        </CardHeader>
        <CardContent>
          <CaseTimeline currentStatus={c.status} />
        </CardContent>
      </Card>

      {showEvidence && (
        <div className="space-y-2">
          <h3 className="text-sm font-medium text-muted-foreground px-1">Evidence</h3>
          <EvidenceUpload
            dealId={c.dealId}
            manifestId={manifestId}
            onManifestCreated={setLocalManifestId}
            readOnly={evidenceReadOnly}
          />
        </div>
      )}

      <DecisionSection c={c} isAdminOrArbitrator={isAdminOrArbitrator} />
      <AppealSection c={c} userId={userId} />
      <AdminActions c={c} isAdminOrArbitrator={isAdminOrArbitrator} />
    </div>
  );
}
