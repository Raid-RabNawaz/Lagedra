import { Badge } from "@/components/ui/badge";
import type { DealPhase } from "@/api/types";

const phaseConfig: Record<DealPhase, { label: string; variant: "default" | "secondary" | "destructive" | "success" | "outline" | "accent" }> = {
  Inquiry: { label: "Inquiry", variant: "secondary" },
  TruthSurface: { label: "Truth Surface", variant: "accent" },
  AwaitingPayment: { label: "Awaiting Payment", variant: "outline" },
  Checkout: { label: "Checkout", variant: "default" },
  Active: { label: "Active", variant: "success" },
  Closed: { label: "Completed", variant: "secondary" },
  Cancelled: { label: "Cancelled", variant: "destructive" },
};

export function DealPhaseBadge({ phase }: { phase: DealPhase }) {
  const config = phaseConfig[phase] ?? { label: phase, variant: "outline" as const };
  return <Badge variant={config.variant}>{config.label}</Badge>;
}
