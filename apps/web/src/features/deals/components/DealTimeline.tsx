import { Check, Circle } from "lucide-react";
import { cn } from "@/lib/utils";
import type { DealPhase } from "@/api/types";
import { dealPhaseLabel } from "@/features/deals/utils/dealVocabulary";

const steps: { phase: DealPhase; label: string }[] = [
  { phase: "TruthSurface", label: dealPhaseLabel("TruthSurface") },
  { phase: "Checkout", label: dealPhaseLabel("Checkout") },
  { phase: "Active", label: dealPhaseLabel("Active") },
  { phase: "AwaitingDepositReturn", label: dealPhaseLabel("AwaitingDepositReturn") },
  { phase: "Closed", label: dealPhaseLabel("Closed") },
];

const phaseOrder: Record<DealPhase, number> = {
  TruthSurface: 0,
  Checkout: 1,
  Active: 2,
  AwaitingDepositReturn: 3,
  Closed: 4,
  PaymentFailed: 1,
  Cancelled: -1,
};

export function DealTimeline({ currentPhase }: { currentPhase: DealPhase }) {
  const currentIdx = phaseOrder[currentPhase] ?? -1;
  const isCancelled = currentPhase === "Cancelled";
  const isPaymentFailed = currentPhase === "PaymentFailed";

  if (isCancelled) {
    return (
      <div className="flex items-center justify-center rounded-lg bg-destructive/10 py-3 px-4 text-sm font-medium text-destructive">
        This deal has been cancelled
      </div>
    );
  }

  if (isPaymentFailed) {
    return (
      <div className="flex items-center justify-center rounded-lg bg-destructive/10 py-3 px-4 text-center text-sm font-medium text-destructive">
        The agreement is sealed, but the deposit payment failed — update your
        card to finish activating this booking.
      </div>
    );
  }

  return (
    <div className="flex items-center gap-1">
      {steps.map((step, idx) => {
        const isCompleted = idx < currentIdx;
        const isCurrent = idx === currentIdx;
        return (
          <div key={step.phase} className="flex items-center flex-1 last:flex-none">
            <div className="flex flex-col items-center gap-1 min-w-[60px]">
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
