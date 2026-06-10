import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  Gavel,
  AlertTriangle,
  ExternalLink,
  UserCheck,
  Scale,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { formatDate, formatMoney } from "@/utils/format";
import { useAuthStore } from "@/app/auth/authStore";
import { isAdmin } from "@/app/auth/permissions";
import { roles } from "@/app/auth/roles";
import { CaseEvidencePanel } from "@/features/arbitration/components/CaseEvidencePanel";
import { CaseTruthSurfaceSection } from "@/features/arbitration/components/CaseTruthSurfaceSection";
import { PartyEvidenceUpload } from "@/features/arbitration/components/PartyEvidenceUpload";
import { VerdictForm } from "@/features/arbitration/components/VerdictForm";
import { CaseWorkflowBanner } from "@/features/arbitration/components/CaseWorkflowBanner";
import { CaseAssignmentPanel } from "@/features/arbitration/components/CaseAssignmentPanel";
import { ArbitratorActions } from "@/features/arbitration/components/ArbitratorActions";
import { useCase, useAppealCase } from "@/features/arbitration/hooks/useArbitration";
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

function statusBadgeVariant(status: string) {
  if (status === "Decided" || status === "Closed") return "success" as const;
  if (status === "Appealed") return "destructive" as const;
  if (status === "UnderReview") return "default" as const;
  if (status === "EvidenceComplete") return "accent" as const;
  return "secondary" as const;
}

function CaseHeader({ c }: { c: CaseDto }) {
  return (
    <Card className="overflow-hidden">
      <div className="h-1 bg-gradient-to-r from-blue-500/80 via-violet-500/60 to-transparent" />
      <CardContent className="p-5 space-y-4">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex items-start gap-3">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
              <Gavel className="h-6 w-6" />
            </div>
            <div>
              <h1 className="font-semibold text-xl tracking-tight">
                {categoryLabel(c.category)}
              </h1>
              <p className="text-sm text-muted-foreground mt-0.5">{tierLabel(c.tier)}</p>
              <div className="flex flex-wrap gap-2 mt-2">
                <Link
                  to={`/app/deals/${c.dealId}`}
                  className="text-xs text-primary hover:underline inline-flex items-center gap-1"
                >
                  View deal
                  <ExternalLink className="h-3 w-3" />
                </Link>
                <span className="text-xs text-muted-foreground font-mono">
                  Case {c.caseId.slice(0, 8)}…
                </span>
              </div>
            </div>
          </div>
          <Badge variant={statusBadgeVariant(c.status)} className="self-start text-sm px-3 py-1">
            {c.status.replace(/([A-Z])/g, " $1").trim()}
          </Badge>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4 text-sm">
          <div>
            <p className="text-muted-foreground text-xs">Filed</p>
            <p className="font-medium">{formatDate(c.filedAt)}</p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Filing fee</p>
            <p className="font-medium">{formatMoney(c.filingFeeCents)}</p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Evidence slots</p>
            <p className="font-medium">{c.evidenceSlotCount}</p>
          </div>
          {c.decisionDueAt && (
            <div>
              <p className="text-muted-foreground text-xs">Decision due</p>
              <p className="font-medium">{formatDate(c.decisionDueAt)}</p>
            </div>
          )}
          <div>
            <p className="text-muted-foreground text-xs">Arbitrator</p>
            <p className="font-medium truncate">
              {c.assignedArbitratorEmail ?? (
                <span className="text-muted-foreground italic">Unassigned</span>
              )}
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function AppealSection({ c, userId }: { c: CaseDto; userId: string }) {
  const appealCase = useAppealCase();
  const [reason, setReason] = useState("");
  const isParty =
    userId === c.filedByUserId
    || userId === c.landlordUserId
    || userId === c.tenantUserId;

  if (c.status !== "Decided" || !isParty) {
    return null;
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <AlertTriangle className="h-4 w-4 text-amber-500" />
          Appeal decision
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
          File appeal
        </Button>
      </CardContent>
    </Card>
  );
}

export function CaseDetailPage() {
  const { caseId } = useParams<{ caseId: string }>();
  const user = useAuthStore((s) => s.user);
  const userId = user?.userId ?? "";
  const { data: c, isLoading, error, refetch } = useCase(caseId);

  const userIsAdmin = isAdmin(user?.role ?? "");
  const userIsArbitrator = String(user?.role) === roles.arbitrator;
  const isAssignedArbitrator =
    userIsArbitrator && Boolean(userId) && c?.assignedArbitratorUserId === userId;
  const canIssueDecision = userIsAdmin || isAssignedArbitrator;

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

  const showEvidence = [
    "Filed",
    "EvidencePending",
    "EvidenceComplete",
    "UnderReview",
    "Decided",
    "Closed",
    "Appealed",
  ].includes(c.status);
  const isDealParty =
    Boolean(userId)
    && (userId === c.filedByUserId
      || userId === c.landlordUserId
      || userId === c.tenantUserId);
  const showReviewerMaterials = userIsAdmin || isAssignedArbitrator;

  return (
    <div className="mx-auto max-w-4xl space-y-6 pb-12">
      <div className="flex items-center justify-between gap-3">
        <Link
          to="/app/arbitration"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          All cases
        </Link>
        {userIsAdmin && (
          <Link to="/app/admin/arbitration-backlog">
            <Button variant="outline" size="sm">
              <Scale className="h-4 w-4 mr-2" />
              Backlog
            </Button>
          </Link>
        )}
      </div>

      <CaseHeader c={c} />

      <div className="flex flex-col-reverse gap-6 lg:grid lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start">
        <aside className="space-y-4 lg:col-start-2 lg:row-start-1">
          <CaseWorkflowBanner c={c} />
          {userIsAdmin && (
            <CaseAssignmentPanel c={c} onUpdated={() => void refetch()} />
          )}
          {userIsArbitrator && (
            <ArbitratorActions c={c} userId={userId} onUpdated={() => void refetch()} />
          )}
        </aside>

        <div className="space-y-6 min-w-0 lg:col-start-1">
          {showEvidence && isDealParty && (
            <section className="space-y-2">
              <h2 className="text-sm font-semibold px-1">Your evidence</h2>
              <PartyEvidenceUpload
                c={c}
                userId={userId}
                onCaseUpdated={() => void refetch()}
              />
            </section>
          )}

          {showEvidence && showReviewerMaterials && (
            <section className="space-y-2">
              <h2 className="text-sm font-semibold px-1 flex items-center gap-2">
                <UserCheck className="h-4 w-4 text-muted-foreground" />
                Case evidence (all parties)
              </h2>
              <CaseEvidencePanel c={c} />
            </section>
          )}

          {showReviewerMaterials && (
            <section className="space-y-2">
              <h2 className="text-sm font-semibold px-1">Truth surface</h2>
              <CaseTruthSurfaceSection dealId={c.dealId} />
            </section>
          )}

          {isAssignedArbitrator && c.status === "EvidenceComplete" && (
            <div className="rounded-xl border border-blue-200 bg-blue-50/50 px-4 py-3 text-sm text-blue-900">
              <strong>Next step:</strong> Review the truth surface and all party evidence, then
              click <strong>Begin review</strong> before issuing a verdict.
            </div>
          )}

          <VerdictForm c={c} canDecide={canIssueDecision} />
          <AppealSection c={c} userId={userId} />
        </div>
      </div>
    </div>
  );
}
