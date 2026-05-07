import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  BookOpen,
  ShieldCheck,
  ShieldAlert,
  Award,
  AlertTriangle,
  CreditCard,
  Scale,
  UserCheck,
  Star,
  XCircle,
  FileWarning,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { formatDate } from "@/utils/format";
import { useDealLedger, useUserLedger } from "@/features/compliance/hooks/useCompliance";
import { useAuthStore } from "@/app/auth/authStore";
import type { TrustLedgerEntryDto, TrustLedgerEntryType } from "@/api/types";

const entryConfig: Record<
  TrustLedgerEntryType,
  { label: string; icon: typeof ShieldCheck; color: string; positive: boolean }
> = {
  DealCompleted: { label: "Deal Completed", icon: Award, color: "text-green-600", positive: true },
  ViolationRecorded: { label: "Violation Recorded", icon: ShieldAlert, color: "text-red-500", positive: false },
  ViolationDismissed: { label: "Violation Dismissed", icon: XCircle, color: "text-slate-500", positive: true },
  ArbitrationRuling: { label: "Arbitration Ruling", icon: Scale, color: "text-blue-600", positive: false },
  InsuranceClaim: { label: "Insurance Claim", icon: FileWarning, color: "text-amber-500", positive: false },
  PaymentDefault: { label: "Payment Default", icon: CreditCard, color: "text-red-500", positive: false },
  EarlyTermination: { label: "Early Termination", icon: AlertTriangle, color: "text-amber-500", positive: false },
  PositiveReview: { label: "Positive Review", icon: Star, color: "text-green-600", positive: true },
  IdentityVerified: { label: "Identity Verified", icon: UserCheck, color: "text-green-600", positive: true },
};

function LedgerEntryRow({ entry }: { entry: TrustLedgerEntryDto }) {
  const config = entryConfig[entry.entryType] ?? {
    label: entry.entryType,
    icon: BookOpen,
    color: "text-muted-foreground",
    positive: false,
  };
  const Icon = config.icon;

  return (
    <div className="flex items-start gap-4 rounded-lg border p-4">
      <div
        className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted ${config.color}`}
      >
        <Icon className="h-4.5 w-4.5" />
      </div>
      <div className="flex-1 min-w-0 space-y-0.5">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="font-medium text-sm">{config.label}</span>
          {config.positive ? (
            <Badge variant="success" className="text-[10px] px-1.5 py-0">
              Positive
            </Badge>
          ) : (
            <Badge variant="destructive" className="text-[10px] px-1.5 py-0">
              Negative
            </Badge>
          )}
          {entry.isPublic && (
            <Badge variant="outline" className="text-[10px] px-1.5 py-0">
              Public
            </Badge>
          )}
        </div>
        {entry.description && (
          <p className="text-xs text-muted-foreground">{entry.description}</p>
        )}
        <p className="text-xs text-muted-foreground">
          {formatDate(entry.occurredAt)}
        </p>
      </div>
    </div>
  );
}

function LedgerList({
  entries,
  emptyMessage,
}: {
  entries: TrustLedgerEntryDto[] | undefined;
  emptyMessage: string;
}) {
  if (!entries || entries.length === 0) {
    return (
      <p className="text-sm text-muted-foreground text-center py-8">
        {emptyMessage}
      </p>
    );
  }

  const sorted = [...entries].sort(
    (a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime(),
  );

  const positiveCount = sorted.filter(
    (e) => entryConfig[e.entryType]?.positive,
  ).length;
  const negativeCount = sorted.length - positiveCount;

  return (
    <div className="space-y-4">
      <div className="flex gap-3 text-sm">
        <span className="flex items-center gap-1 text-green-600">
          <ShieldCheck className="h-4 w-4" /> {positiveCount} positive
        </span>
        <span className="flex items-center gap-1 text-red-500">
          <ShieldAlert className="h-4 w-4" /> {negativeCount} negative
        </span>
      </div>
      <div className="space-y-2">
        {sorted.map((entry) => (
          <LedgerEntryRow key={entry.id} entry={entry} />
        ))}
      </div>
    </div>
  );
}

export function DealTrustLedgerPage() {
  const { dealId } = useParams<{ dealId: string }>();
  const { data: entries, isLoading, error } = useDealLedger(dealId);

  if (isLoading) {
    return <Loader label="Loading trust ledger..." />;
  }

  if (error) {
    return (
      <EmptyState
        title="Ledger unavailable"
        description="Could not load the trust ledger for this deal."
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
        <BookOpen className="h-7 w-7 text-blue-600" />
        <div>
          <h1 className="text-xl font-bold tracking-tight">Deal Trust Ledger</h1>
          <p className="text-sm text-muted-foreground">
            Permanent record of compliance-relevant events for this deal.
          </p>
        </div>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Ledger Entries</CardTitle>
        </CardHeader>
        <CardContent>
          <LedgerList
            entries={entries}
            emptyMessage="No ledger entries for this deal yet."
          />
        </CardContent>
      </Card>
    </div>
  );
}

export function UserTrustLedgerPage() {
  const user = useAuthStore((s) => s.user);
  const userId = user?.userId;
  const { data: entries, isLoading, error } = useUserLedger(userId);

  if (isLoading) {
    return <Loader label="Loading your trust ledger..." />;
  }

  if (error) {
    return (
      <EmptyState
        title="Ledger unavailable"
        description="Could not load your trust ledger."
      >
        <Link to="/app">
          <Button variant="outline" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Dashboard
          </Button>
        </Link>
      </EmptyState>
    );
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link
        to="/app"
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ArrowLeft className="h-4 w-4" />
        Dashboard
      </Link>

      <div className="flex items-center gap-3">
        <BookOpen className="h-7 w-7 text-blue-600" />
        <div>
          <h1 className="text-xl font-bold tracking-tight">My Trust Ledger</h1>
          <p className="text-sm text-muted-foreground">
            Your permanent trust record across all deals on the platform.
          </p>
        </div>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Your Entries</CardTitle>
        </CardHeader>
        <CardContent>
          <LedgerList
            entries={entries}
            emptyMessage="Your trust ledger is clean — no entries yet."
          />
        </CardContent>
      </Card>
    </div>
  );
}
