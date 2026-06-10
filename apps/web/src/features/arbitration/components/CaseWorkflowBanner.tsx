import { CheckCircle2, Circle, AlertCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import type { CaseDto } from "@/api/types";

type Step = {
  key: string;
  label: string;
  done: boolean;
  current: boolean;
  hint?: string;
};

function buildSteps(c: CaseDto): Step[] {
  const hasEvidence = c.evidenceSlotCount > 0;
  const assigned = Boolean(c.assignedArbitratorUserId);
  const reviewReady =
    c.status === "EvidenceComplete" || Boolean(c.evidenceCompleteAt);
  const underReview = c.status === "UnderReview";
  const decided = c.status === "Decided" || c.status === "Closed";
  const appealed = c.status === "Appealed";

  return [
    {
      key: "filed",
      label: "Case filed",
      done: true,
      current: c.status === "Filed",
    },
    {
      key: "evidence",
      label: "Party evidence submitted",
      done: Boolean(hasEvidence && (reviewReady || underReview || decided)),
      current: Boolean(
        c.status === "EvidencePending" || (c.status === "Filed" && hasEvidence),
      ),
      hint: hasEvidence
        ? `${c.evidenceSlotCount} slot(s) on record`
        : "Parties must seal & submit manifests",
    },
    {
      key: "ready",
      label: "Review ready",
      done: reviewReady || underReview || decided,
      current: c.status === "EvidenceComplete",
      hint: "Admin can assign an arbitrator",
    },
    {
      key: "assigned",
      label: "Arbitrator assigned",
      done: assigned,
      current: Boolean(reviewReady && !assigned),
      hint: assigned ? (c.assignedArbitratorEmail ?? "Assigned") : "Awaiting assignment",
    },
    {
      key: "review",
      label: "Under review",
      done: underReview || decided,
      current: assigned && c.status === "EvidenceComplete",
      hint: "Arbitrator begins review when ready",
    },
    {
      key: "verdict",
      label: "Verdict issued",
      done: decided,
      current: underReview,
    },
    ...(appealed
      ? [
          {
            key: "appeal",
            label: "Appeal — new evidence round",
            done: false,
            current: true,
            hint: "Parties must submit new sealed evidence",
          },
        ]
      : []),
  ];
}

export function CaseWorkflowBanner({ c }: { c: CaseDto }) {
  const steps = buildSteps(c);
  const blocked = !c.assignedArbitratorUserId && c.status === "UnderReview";

  return (
    <div className="rounded-xl border bg-card p-4 space-y-3">
      <p className="text-sm font-medium">Case workflow</p>
      <ul className="space-y-2">
        {steps.map((step) => (
          <li key={step.key} className="flex gap-3 text-sm">
            {step.done ? (
              <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-600 mt-0.5" />
            ) : step.current ? (
              <Circle className="h-4 w-4 shrink-0 text-primary mt-0.5 fill-primary/20" />
            ) : (
              <Circle className="h-4 w-4 shrink-0 text-muted-foreground/40 mt-0.5" />
            )}
            <div className="min-w-0">
              <span
                className={cn(
                  "font-medium",
                  step.current && "text-primary",
                  step.done && "text-foreground",
                  !step.done && !step.current && "text-muted-foreground",
                )}
              >
                {step.label}
              </span>
              {step.hint && (
                <p className="text-xs text-muted-foreground mt-0.5">{step.hint}</p>
              )}
            </div>
          </li>
        ))}
      </ul>
      {blocked && (
        <p className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2 flex gap-2">
          <AlertCircle className="h-4 w-4 shrink-0" />
          Status is Under Review but no arbitrator is assigned. Assign from the panel below.
        </p>
      )}
      {c.status === "EvidencePending" && (
        <p className="text-xs text-muted-foreground">
          Mark evidence complete (admin) or wait for both parties to submit sealed evidence.
        </p>
      )}
    </div>
  );
}
