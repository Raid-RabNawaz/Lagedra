import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  Scale,
  ArrowRight,
  Gavel,
  FileText,
  Clock,
  Plus,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { formatDate, formatMoney } from "@/utils/format";
import { useCases } from "@/features/arbitration/hooks/useArbitration";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { FileArbitrationDialog } from "@/features/arbitration/components/FileArbitrationDialog";
import { Select } from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import type { ArbitrationStatus, CaseDto, ArbitrationTier } from "@/api/types";

const statusTabs: { value: ArbitrationStatus; label: string }[] = [
  { value: "Filed", label: "Filed" },
  { value: "EvidencePending", label: "Evidence" },
  { value: "UnderReview", label: "Under Review" },
  { value: "Decided", label: "Decided" },
  { value: "Appealed", label: "Appealed" },
  { value: "Closed", label: "Closed" },
];

function statusBadge(status: ArbitrationStatus) {
  const variants: Record<ArbitrationStatus, { variant: "default" | "secondary" | "destructive" | "success" | "accent" | "outline"; label: string }> = {
    Filed: { variant: "accent", label: "Filed" },
    EvidencePending: { variant: "secondary", label: "Evidence Pending" },
    EvidenceComplete: { variant: "secondary", label: "Evidence Complete" },
    UnderReview: { variant: "default", label: "Under Review" },
    Decided: { variant: "success", label: "Decided" },
    Appealed: { variant: "destructive", label: "Appealed" },
    Closed: { variant: "outline", label: "Closed" },
  };
  const v = variants[status];
  return <Badge variant={v.variant}>{v.label}</Badge>;
}

function tierLabel(tier: ArbitrationTier) {
  return tier === "BindingArbitration" ? "Binding" : "Protocol";
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

function CaseCard({ c }: { c: CaseDto }) {
  return (
    <Link to={`/app/arbitration/${c.caseId}`}>
      <Card className="transition hover:shadow-md group cursor-pointer">
        <CardContent className="flex items-center gap-4 p-4">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground group-hover:bg-primary/10 group-hover:text-primary transition-colors">
            <Gavel className="h-5 w-5" />
          </div>
          <div className="flex-1 min-w-0 space-y-1">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-medium text-sm">
                {categoryLabel(c.category)}
              </span>
              {statusBadge(c.status)}
              <Badge variant="outline" className="text-[10px] px-1.5 py-0">
                {tierLabel(c.tier)}
              </Badge>
            </div>
            <div className="flex items-center gap-3 text-xs text-muted-foreground">
              <span className="flex items-center gap-1">
                <Clock className="h-3 w-3" />
                Filed {formatDate(c.filedAt)}
              </span>
              <span>Fee: {formatMoney(c.filingFeeCents)}</span>
              <span>{c.evidenceSlotCount} evidence slot{c.evidenceSlotCount !== 1 ? "s" : ""}</span>
            </div>
          </div>
          <ArrowRight className="h-4 w-4 text-muted-foreground shrink-0" />
        </CardContent>
      </Card>
    </Link>
  );
}

function DealPickerDialog({
  open,
  onOpenChange,
  onDealSelected,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onDealSelected: (dealId: string) => void;
}) {
  const { data: deals, isLoading } = useMyDeals("all");
  const [selectedDealId, setSelectedDealId] = useState("");

  const eligibleDeals = deals?.filter(
    (d) => d.dealPhase === "Active" || d.dealPhase === "Closed",
  );

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Scale className="h-5 w-5 text-blue-600" />
            Select a Deal
          </DialogTitle>
          <DialogDescription>
            Choose which deal you want to file an arbitration case for.
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <Loader label="Loading deals..." />
        ) : !eligibleDeals || eligibleDeals.length === 0 ? (
          <p className="text-sm text-muted-foreground py-4 text-center">
            No eligible deals found. Only active or closed deals can have
            arbitration cases filed against them.
          </p>
        ) : (
          <Select
            value={selectedDealId}
            onChange={(e) => setSelectedDealId(e.target.value)}
          >
            <option value="">Select a deal...</option>
            {eligibleDeals.map((d) => (
              <option key={d.dealId} value={d.dealId}>
                {d.listingTitle} — {d.dealPhase}
              </option>
            ))}
          </Select>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button
            disabled={!selectedDealId}
            onClick={() => {
              onOpenChange(false);
              onDealSelected(selectedDealId);
            }}
          >
            Continue
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function CaseListPage() {
  const [searchParams] = useSearchParams();
  const [activeTab, setActiveTab] = useState<ArbitrationStatus>("Filed");
  const { data: cases, isLoading, error } = useCases(activeTab);

  const preselectedDealId = searchParams.get("dealId");

  const [dealPickerOpen, setDealPickerOpen] = useState(false);
  const [fileDealId, setFileDealId] = useState<string | null>(
    preselectedDealId,
  );
  const [fileDialogOpen, setFileDialogOpen] = useState(
    Boolean(preselectedDealId),
  );

  const handleFileClick = () => {
    if (preselectedDealId) {
      setFileDealId(preselectedDealId);
      setFileDialogOpen(true);
    } else {
      setDealPickerOpen(true);
    }
  };

  const handleDealSelected = (dealId: string) => {
    setFileDealId(dealId);
    setFileDialogOpen(true);
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Scale className="h-7 w-7 text-blue-600" />
          <div>
            <h1 className="text-xl font-bold tracking-tight">Arbitration Cases</h1>
            <p className="text-sm text-muted-foreground">
              View and manage dispute resolution cases.
            </p>
          </div>
        </div>
        <Button onClick={handleFileClick} className="gap-1.5">
          <Plus className="h-4 w-4" />
          File a Case
        </Button>
      </div>

      <DealPickerDialog
        open={dealPickerOpen}
        onOpenChange={setDealPickerOpen}
        onDealSelected={handleDealSelected}
      />

      {fileDealId && (
        <FileArbitrationDialog
          dealId={fileDealId}
          open={fileDialogOpen}
          onOpenChange={(open) => {
            setFileDialogOpen(open);
            if (!open) setFileDealId(preselectedDealId);
          }}
        />
      )}

      <div className="flex gap-1 overflow-x-auto pb-1">
        {statusTabs.map((tab) => (
          <Button
            key={tab.value}
            variant={activeTab === tab.value ? "default" : "ghost"}
            size="sm"
            onClick={() => setActiveTab(tab.value)}
            className="shrink-0"
          >
            {tab.label}
          </Button>
        ))}
      </div>

      {isLoading && <Loader label="Loading cases..." />}

      {error && (
        <EmptyState
          title="Error loading cases"
          description="Could not fetch arbitration cases. Please try again."
        />
      )}

      {!isLoading && !error && cases?.length === 0 && (
        <EmptyState
          title="No cases"
          description={`No ${activeTab.toLowerCase()} cases found.`}
        >
          <FileText className="h-12 w-12 text-muted-foreground/40" />
        </EmptyState>
      )}

      {cases && cases.length > 0 && (
        <div className="space-y-3">
          {cases.map((c) => (
            <CaseCard key={c.caseId} c={c} />
          ))}
        </div>
      )}
    </div>
  );
}
