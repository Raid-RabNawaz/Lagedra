import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  ShieldCheck,
  ShieldAlert,
  ShieldX,
  AlertTriangle,
  CheckCircle2,
  Clock,
  Activity,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { useAuthStore } from "@/app/auth/authStore";
import { isAdmin } from "@/app/auth/permissions";
import { formatDate } from "@/utils/format";
import {
  useComplianceStatus,
  useDealViolations,
  useCureViolation,
} from "@/features/compliance/hooks/useCompliance";
import type { MonitoredViolationDto, MonitoredViolationStatus } from "@/api/types";

function violationStatusBadge(status: MonitoredViolationStatus) {
  switch (status) {
    case "Open":
      return <Badge variant="destructive">Open</Badge>;
    case "Cured":
      return <Badge variant="success">Cured</Badge>;
    case "Escalated":
      return (
        <Badge className="border-transparent bg-amber-500 text-white">
          Escalated
        </Badge>
      );
  }
}

function violationCategoryLabel(category: string) {
  const labels: Record<string, string> = {
    CategoryA: "Insurance Lapse",
    CategoryB: "Payment Default",
    CategoryC: "Lease Violation",
  };
  return labels[category] ?? category;
}

function ViolationRow({
  violation,
  showCure,
  onCure,
  curing,
}: {
  violation: MonitoredViolationDto;
  showCure: boolean;
  onCure: (id: string) => void;
  curing: boolean;
}) {
  return (
    <div className="flex items-start justify-between gap-4 rounded-lg border p-4">
      <div className="space-y-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="font-medium text-sm">
            {violationCategoryLabel(violation.category)}
          </span>
          {violationStatusBadge(violation.status)}
        </div>
        <p className="text-xs text-muted-foreground">
          Detected {formatDate(violation.detectedAt)}
        </p>
        {violation.cureDeadline && (
          <p className="text-xs text-muted-foreground flex items-center gap-1">
            <Clock className="h-3 w-3" />
            Cure by {formatDate(violation.cureDeadline)}
          </p>
        )}
      </div>
      {showCure && violation.status === "Open" && (
        <Button
          size="sm"
          variant="outline"
          disabled={curing}
          onClick={() => onCure(violation.violationId)}
        >
          <CheckCircle2 className="h-3.5 w-3.5 mr-1.5" />
          Cure
        </Button>
      )}
    </div>
  );
}

export function DealCompliancePage() {
  const { dealId } = useParams<{ dealId: string }>();
  const user = useAuthStore((s) => s.user);
  const admin = isAdmin(user?.role ?? "");

  const {
    data: status,
    isLoading: statusLoading,
    error: statusError,
  } = useComplianceStatus(dealId);

  const {
    data: violations,
    isLoading: violationsLoading,
  } = useDealViolations(dealId);

  const cureMutation = useCureViolation();

  if (statusLoading || violationsLoading) {
    return <Loader label="Loading compliance status..." />;
  }

  if (statusError || !status) {
    return (
      <EmptyState
        title="Compliance data unavailable"
        description="Could not load compliance information for this deal."
      >
        <Link to={`/app/deals/${dealId}`}>
          <Button variant="outline" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to deal
          </Button>
        </Link>
      </EmptyState>
    );
  }

  const ComplianceIcon = status.isCompliant ? ShieldCheck : ShieldAlert;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link
        to={`/app/deals/${dealId}`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to deal
      </Link>

      <div className="flex items-center gap-3">
        <ComplianceIcon
          className={`h-7 w-7 ${status.isCompliant ? "text-green-600" : "text-red-500"}`}
        />
        <div>
          <h1 className="text-xl font-bold tracking-tight">
            Deal Compliance
          </h1>
          <p className="text-sm text-muted-foreground">
            {status.isCompliant
              ? "This deal is currently compliant."
              : "This deal has compliance issues that need attention."}
          </p>
        </div>
      </div>

      {/* Status overview cards */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <Card>
          <CardContent className="p-4 text-center">
            <ShieldX className="h-5 w-5 mx-auto text-red-500 mb-1" />
            <p className="text-2xl font-bold">{status.openViolations}</p>
            <p className="text-xs text-muted-foreground">Open</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 text-center">
            <CheckCircle2 className="h-5 w-5 mx-auto text-green-600 mb-1" />
            <p className="text-2xl font-bold">{status.curedViolations}</p>
            <p className="text-xs text-muted-foreground">Cured</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 text-center">
            <AlertTriangle className="h-5 w-5 mx-auto text-amber-500 mb-1" />
            <p className="text-2xl font-bold">{status.escalatedViolations}</p>
            <p className="text-xs text-muted-foreground">Escalated</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4 text-center">
            <Activity className="h-5 w-5 mx-auto text-blue-500 mb-1" />
            <p className="text-2xl font-bold">{status.totalSignals}</p>
            <p className="text-xs text-muted-foreground">
              Signals ({status.unprocessedSignals} pending)
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Violations list */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Violations</CardTitle>
        </CardHeader>
        <CardContent>
          {!violations || violations.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-6">
              No violations recorded for this deal.
            </p>
          ) : (
            <div className="space-y-3">
              {violations.map((v) => (
                <ViolationRow
                  key={v.violationId}
                  violation={v}
                  showCure={admin}
                  onCure={(vid) =>
                    cureMutation.mutate({ dealId: dealId!, violationId: vid })
                  }
                  curing={cureMutation.isPending}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Link to full trust ledger for this deal */}
      <div className="text-center">
        <Link to={`/app/deals/${dealId}/trust-ledger`}>
          <Button variant="outline" size="sm" className="gap-2">
            <Activity className="h-4 w-4" />
            View Trust Ledger
          </Button>
        </Link>
      </div>
    </div>
  );
}
