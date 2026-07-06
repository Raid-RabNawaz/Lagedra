import { Check, Circle, AlertTriangle } from "lucide-react";
import { cn } from "@/lib/utils";
import type { ArbitrationStatus } from "@/api/types";

const steps: { status: ArbitrationStatus; label: string }[] = [
  { status: "Filed", label: "Filed" },
  { status: "EvidencePending", label: "Evidence" },
  { status: "EvidenceComplete", label: "Review Ready" },
  { status: "UnderReview", label: "Under Review" },
  { status: "Decided", label: "Decided" },
  { status: "Closed", label: "Closed" },
];

const statusOrder: Record<ArbitrationStatus, number> = {
  PendingPayment: -1,
  Filed: 0,
  EvidencePending: 1,
  EvidenceComplete: 2,
  UnderReview: 3,
  Decided: 4,
  Appealed: -1,
  Closed: 5,
};

export function CaseTimeline({ currentStatus }: { currentStatus: ArbitrationStatus }) {
  const isAppealed = currentStatus === "Appealed";
  const currentIdx = statusOrder[currentStatus] ?? -1;

  if (isAppealed) {
    return (
      <div className="space-y-3">
        <div className="flex items-center justify-center gap-2 rounded-lg bg-amber-50 border border-amber-200 py-3 px-4 text-sm font-medium text-amber-800">
          <AlertTriangle className="h-4 w-4" />
          This case has been appealed and is under further review
        </div>
        <TimelineBar currentIdx={4} />
      </div>
    );
  }

  return <TimelineBar currentIdx={currentIdx} />;
}

function TimelineBar({ currentIdx }: { currentIdx: number }) {
  return (
    <div className="flex items-center gap-1">
      {steps.map((step, idx) => {
        const isCompleted = idx < currentIdx;
        const isCurrent = idx === currentIdx;
        return (
          <div key={step.status} className="flex items-center flex-1 last:flex-none">
            <div className="flex flex-col items-center gap-1 min-w-[56px]">
              <div
                className={cn(
                  "flex h-7 w-7 items-center justify-center rounded-full border-2 text-xs font-semibold transition-colors",
                  isCompleted && "border-primary bg-primary text-primary-foreground",
                  isCurrent && "border-primary text-primary",
                  !isCompleted && !isCurrent && "border-muted-foreground/30 text-muted-foreground/50",
                )}
              >
                {isCompleted ? <Check className="h-4 w-4" /> : <Circle className="h-3 w-3" />}
              </div>
              <span
                className={cn(
                  "text-[11px] leading-tight text-center",
                  (isCompleted || isCurrent) ? "text-foreground font-medium" : "text-muted-foreground",
                )}
              >
                {step.label}
              </span>
            </div>
            {idx < steps.length - 1 && (
              <div
                className={cn(
                  "h-0.5 flex-1 mx-1 mt-[-18px]",
                  isCompleted ? "bg-primary" : "bg-muted-foreground/20",
                )}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}
